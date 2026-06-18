import { Injectable, ConflictException, NotFoundException, BadRequestException } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';
import * as path from 'path';
import * as fs from 'fs';
import sharp from 'sharp';
import { v4 as uuidv4 } from 'uuid';
import { ConfigService } from '@nestjs/config';

@Injectable()
export class DispatchService {
  constructor(
    private db: DatabaseService,
    private config: ConfigService,
  ) {}

  async validate(hijoId: number, jugueteId: number, eventoId: number) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('hijoId', sql.Int, hijoId);
    request.input('jugueteId', sql.Int, jugueteId);
    request.input('eventoId', sql.Int, eventoId);

    const result = await request.query(`
      SELECT
        (SELECT StockActual FROM dbo.tblCatalogoJuguetes WHERE Id = @jugueteId AND Activo = 1) AS stockActual,
        (SELECT COUNT(1) FROM dbo.tblEntregasJuguetes WHERE HijoId = @hijoId AND EventoId = @eventoId AND Estado = 'DELIVERED') AS entregasActivas,
        (SELECT h.CarnetColaborador FROM dbo.tblHijos h WHERE h.Id = @hijoId) AS carnetColaborador
    `);

    const data = result.recordset[0];
    if (!data) {
      throw new NotFoundException('No se pudieron validar los datos.');
    }

    const carnet = data.carnetColaborador;
    let asistio = false;
    if (carnet) {
      const asisReq = pool.request();
      asisReq.input('carnet', sql.VarChar(50), carnet);
      asisReq.input('eventoId', sql.Int, eventoId);
      const asisResult = await asisReq.query(`
        SELECT COUNT(1) AS cnt FROM dbo.tblAsistenciaEventos
        WHERE CarnetColaborador = @carnet AND EventoId = @eventoId
      `);
      asistio = asisResult.recordset[0]?.cnt > 0;
    }

    return {
      stockDisponible: (data.stockActual || 0) > 0,
      stockActual: data.stockActual || 0,
      hijoYaEntregado: data.entregasActivas > 0,
      colaboradorAsistio: asistio,
      esValido: (data.stockActual || 0) > 0 && data.entregasActivas === 0 && asistio,
    };
  }

  async deliver(
    dto: { eventoId: number; hijoId: number; jugueteId: number; carnetColaborador: string; recibidoPor: string; nombreReceptor?: string },
    usuarioDespacho: string,
    fotoFile?: Express.Multer.File,
  ) {
    const pool = this.db.getPool();

    let fotoEvidenciaUrl: string | null = null;

    if (fotoFile) {
      const uploadPath = this.config.get<string>('UPLOAD_PATH', './uploads');
      const fotosDir = path.join(uploadPath, 'fotos_evidencia');
      if (!fs.existsSync(fotosDir)) {
        fs.mkdirSync(fotosDir, { recursive: true });
      }

      const fileName = `entrega_${uuidv4()}.webp`;
      const filePath = path.join(fotosDir, fileName);

      try {
        await sharp(fotoFile.buffer)
          .resize({ width: 1024, withoutEnlargement: true })
          .webp({ quality: 80 })
          .toFile(filePath);

        fotoEvidenciaUrl = `/uploads/fotos_evidencia/${fileName}`;
      } catch {
        throw new BadRequestException('Error al procesar la imagen.');
      }
    }

    const request = pool.request();
    request.input('EventoId', sql.Int, dto.eventoId);
    request.input('HijoId', sql.Int, dto.hijoId);
    request.input('JugueteId', sql.Int, dto.jugueteId);
    request.input('CarnetColaborador', sql.VarChar(50), dto.carnetColaborador);
    request.input('RecibidoPor', sql.VarChar(20), dto.recibidoPor);
    request.input('NombreReceptor', sql.VarChar(250), dto.nombreReceptor || null);
    request.input('FotoEvidenciaUrl', sql.VarChar(500), fotoEvidenciaUrl);
    request.input('UsuarioDespacho', sql.VarChar(100), usuarioDespacho);
    request.output('EntregaId', sql.Int);

    try {
      await request.execute('sp_Despacho_Entregar');
      const entregaId = request.output.EntregaId;

      const stockReq = pool.request();
      stockReq.input('id', sql.Int, dto.jugueteId);
      const stockResult = await stockReq.query(`
        SELECT StockActual FROM dbo.tblCatalogoJuguetes WHERE Id = @id
      `);

      return {
        entregaId,
        stockRestante: stockResult.recordset[0]?.StockActual || 0,
      };
    } catch (err: any) {
      const msg = (err.message || '').trim();
      if (msg.includes('no existe') || msg.includes('no esta activo')) throw new NotFoundException(msg || 'Recurso no encontrado');
      if (msg.includes('stock')) throw new ConflictException(msg || 'Stock insuficiente');
      if (msg.includes('ya tiene una entrega')) throw new ConflictException(msg || 'El hijo ya tiene una entrega activa');
      throw err;
    }
  }

  async revert(entregaId: number, usuarioReversion: string, motivo: string) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('EntregaId', sql.Int, entregaId);
    request.input('UsuarioReversion', sql.VarChar(100), usuarioReversion);
    request.input('MotivoReversion', sql.VarChar(500), motivo);

    try {
      await request.execute('sp_Despacho_Reversar');
      return { success: true };
    } catch (err: any) {
      const msg = (err.message || '').trim();
      if (msg.includes('no existe')) throw new NotFoundException('La entrega especificada no existe.');
      if (msg.includes('ya se encuentra reversada')) throw new ConflictException('Esta entrega ya se encuentra reversada.');
      throw err;
    }
  }

  async getAuditoria(eventoId: number, busqueda?: string, pagina = 1, porPagina = 50) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('eventoId', sql.Int, eventoId);

    let where = 'WHERE EventoId = @eventoId';
    if (busqueda) {
      request.input('busqueda', sql.VarChar(250), `%${busqueda}%`);
      where += ` AND (ColaboradorNombre LIKE @busqueda OR ColaboradorCarnet LIKE @busqueda OR HijoNombre LIKE @busqueda)`;
    }

    const offset = (pagina - 1) * porPagina;

    const countResult = await request.query(`
      SELECT COUNT(*) AS total FROM dbo.vw_BitacoraEntregas
      ${where}
    `);
    const total = countResult.recordset[0]?.total || 0;

    const dataResult = await request.query(`
      SELECT * FROM dbo.vw_BitacoraEntregas
      ${where}
      ORDER BY FechaEntrega DESC
      OFFSET ${offset} ROWS FETCH NEXT ${porPagina} ROWS ONLY
    `);

    return {
      data: dataResult.recordset.map((r: any) => ({
        entregaId: r.EntregaId,
        eventoId: r.EventoId,
        eventoNombre: r.EventoNombre,
        colaboradorCarnet: r.ColaboradorCarnet,
        colaboradorNombre: r.ColaboradorNombre,
        colaboradorGerencia: r.ColaboradorGerencia,
        hijoNombre: r.HijoNombre,
        hijoEdad: r.HijoEdad,
        hijoGenero: r.HijoGenero,
        categoria: r.Categoria,
        nombreJuguete: r.NombreJuguete,
        estado: r.Estado,
        recibidoPor: r.RecibidoPor,
        receptorFinal: r.ReceptorFinal,
        fotoEvidenciaUrl: r.FotoEvidenciaUrl,
        fechaEntrega: r.FechaEntrega,
        usuarioDespacho: r.UsuarioDespacho,
        fechaReversion: r.FechaReversion,
        usuarioReversion: r.UsuarioReversion,
        motivoReversion: r.MotivoReversion,
      })),
      total,
      pagina,
      porPagina,
      totalPaginas: Math.ceil(total / porPagina),
    };
  }
}
