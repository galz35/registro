import { Injectable, NotFoundException, ConflictException } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import { HcmService } from '../integration/hcm.service';
import * as sql from 'mssql';

@Injectable()
export class AttendanceService {
  constructor(
    private db: DatabaseService,
    private hcm: HcmService,
  ) {}

  async searchCollaborators(query: string): Promise<any[]> {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('q', sql.VarChar(100), `%${query}%`);
    // Search in local table
    const local = await request.query(`
      SELECT Carnet, Nombre, Gerencia, Ubicacion FROM dbo.tblColaboradores
      WHERE Activo = 1 AND (Carnet LIKE @q OR Nombre LIKE @q)
    `);
    // Search in Portal if few results
    let portal: any[] = [];
    if (local.recordset.length < 10) {
      try {
        const portalReq = pool.request();
        portalReq.input('q', sql.VarChar(100), `%${query}%`);
        const pResult = await portalReq.query(`
          SELECT carnet AS Carnet, nombreCompleto AS Nombre, orgGerencia AS Gerencia, ubicacion AS Ubicacion
          FROM bdplaner.dbo.p_Usuarios
          WHERE activo = 1 AND (carnet LIKE @q OR nombreCompleto LIKE @q)
        `);
        portal = pResult.recordset || [];
      } catch {}
    }
    // Merge, avoid duplicates (local has priority)
    const seen = new Set(local.recordset.map((r: any) => r.Carnet));
    const all = [...local.recordset, ...portal.filter((p: any) => !seen.has(p.Carnet))];
    return all.slice(0, 15).map((r: any) => ({
      carnet: r.Carnet, nombre: r.Nombre,
      gerencia: r.Gerencia || null, ubicacion: r.Ubicacion || null,
    }));
  }

  async buscarEnPortal(carnet: string): Promise<any | null> {
    try {
      const request = this.db.getPool().request();
      request.input('carnet', sql.VarChar(50), carnet);
      const result = await request.query(`
        SELECT idUsuario, nombre, nombreCompleto, correo, activo, carnet, cargo, departamento,
               orgGerencia AS gerencia, ubicacion, genero, fechaIngreso
        FROM bdplaner.dbo.p_Usuarios
        WHERE carnet = @carnet
      `);
      const u = result.recordset[0];
      if (!u) return null;
      const inactivo = u.activo === 0 || u.activo === false;
      return {
        Carnet: u.carnet,
        Nombre: u.nombreCompleto || u.nombre,
        Puesto: u.cargo || null,
        Gerencia: u.gerencia || u.departamento || null,
        Ubicacion: u.ubicacion || null,
        Edificio: null,
        inactivo,
      };
    } catch { return null; }
  }

  async lookupCarnet(carnet: string, eventoId: number) {
    const pool = this.db.getPool();

    const request = pool.request();
    request.input('carnet', sql.VarChar(50), carnet);
    request.input('eventoId', sql.Int, eventoId);

    const result = await request.execute('sp_Asistencia_LookupCarnet');

    let rawColab = result.recordsets[0]?.[0] || null;

    // Si no se encuentra localmente, buscar en el Portal
    if (!rawColab) {
      const portalUser = await this.buscarEnPortal(carnet);
      if (portalUser) {
        // Insertarlo temporalmente en tblColaboradores
        const insertReq = pool.request();
        insertReq.input('carnet', sql.VarChar(50), portalUser.Carnet);
        insertReq.input('nombre', sql.VarChar(250), portalUser.Nombre);
        insertReq.input('puesto', sql.VarChar(250), portalUser.Puesto);
        insertReq.input('gerencia', sql.VarChar(250), portalUser.Gerencia);
        insertReq.input('ubicacion', sql.VarChar(250), portalUser.Ubicacion);
        await insertReq.query(`
          IF NOT EXISTS (SELECT 1 FROM tblColaboradores WHERE Carnet = @carnet)
            INSERT INTO tblColaboradores (Carnet, Nombre, Puesto, Gerencia, Ubicacion)
            VALUES (@carnet, @nombre, @puesto, @gerencia, @ubicacion)
        `);
        // Re-ejecutar el SP para obtener respuesta completa
        const result2 = await pool.request()
          .input('carnet', sql.VarChar(50), carnet)
          .input('eventoId', sql.Int, eventoId)
          .execute('sp_Asistencia_LookupCarnet');
        if (result2.recordsets[0]?.[0]) {
          rawColab = result2.recordsets[0][0];
          result.recordsets = result2.recordsets;
        }
      }
    }

    if (!rawColab) {
      throw new NotFoundException('Colaborador no encontrado.');
    }

    const colaborador = {
      carnet: rawColab.Carnet,
      nombre: rawColab.Nombre,
      puesto: rawColab.Puesto || null,
      gerencia: rawColab.Gerencia || null,
      ubicacion: rawColab.Ubicacion || null,
      edificio: rawColab.Edificio || null,
      departamentoGeografico: rawColab.DepartamentoGeografico || null,
      inactivo: false,
    };

    const asistencia = result.recordsets[1]?.[0] || null;
    const hijos = (result.recordsets[2] || []).map((h: any) => ({
      id: h.Id,
      nombreHijo: h.NombreHijo,
      edadHijo: h.EdadHijo,
      generoHijo: h.GeneroHijo,
      categoria: h.Categoria,
      estadoEntrega: h.EstadoEntrega || null,
      entregaId: h.EntregaId || null,
      fechaEntrega: h.FechaEntrega || null,
      recibidoPor: h.RecibidoPor || null,
      fotoEvidenciaUrl: h.FotoEvidenciaUrl || null,
      jugueteSugerido: h.JugueteSugeridoId ? {
        id: h.JugueteSugeridoId,
        nombreJuguete: h.JugueteSugeridoNombre,
        stockActual: h.JugueteStock,
        fotoUrl: h.JugueteFotoUrl,
      } : null,
    }));

    const fotoHcm = await this.hcm.obtenerFotoEmpleado(carnet);
    const familiaresHcm = await this.hcm.obtenerFamiliares(carnet);

    return {
      colaborador,
      inactivo: colaborador.inactivo || false,
      asistio: !!asistencia,
      fechaAsistencia: asistencia?.FechaRegistro || null,
      adultos: asistencia?.Adultos ?? 1,
      ninos: asistencia?.Ninos ?? 0,
      asistioPor: asistencia?.AsistioPor || null,
      nombreAsistente: asistencia?.NombreAsistente || null,
      fotoHcm,
      hijos,
      familiaresHcm,
    };
  }

  async register(dto: { eventoId: number; carnet: string; adultos?: number; ninos?: number; asistioPor?: string; nombreAsistente?: string }, registradoPor: string) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('EventoId', sql.Int, dto.eventoId);
    request.input('CarnetColaborador', sql.VarChar(50), dto.carnet);
    request.input('RegistradoPor', sql.VarChar(100), registradoPor);
    request.input('Adultos', sql.Int, dto.adultos ?? 1);
    request.input('Ninos', sql.Int, dto.ninos ?? 0);
    request.input('AsistioPor', sql.VarChar(20), dto.asistioPor || null);
    request.input('NombreAsistente', sql.VarChar(250), dto.nombreAsistente || null);

    try {
      const result = await request.execute('sp_Asistencia_Registrar');
      const r = result.recordset?.[0];
      if (!r) return { success: true };
      return { id: r.Id, eventoId: r.EventoId, carnet: r.CarnetColaborador, fecha: r.FechaRegistro, registradoPor: r.RegistradoPor };
    } catch (err: any) {
      const msg = (err.message || '').trim();
      if (msg.includes('ya tiene asistencia registrada') || msg.includes('51001')) {
        throw new ConflictException('El colaborador ya tiene asistencia registrada para este evento.');
      }
      if (msg.includes('no existe') || msg.includes('no esta activo') || msg.includes('51000')) {
        throw new NotFoundException('El colaborador no existe o no esta activo.');
      }
      throw err;
    }
  }

  async getSummary(eventoId: number) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('EventoId', sql.Int, eventoId);

    const result = await request.execute('sp_Dashboard_ResumenEvento');

    const kpis = result.recordsets[0]?.[0] || {
      TotalNinos: 0, TotalColaboradores: 0, Asistidos: 0,
      Entregados: 0, Reversados: 0, Pendientes: 0, PorcentajeAvance: 0,
    };

    const stockCritico = result.recordsets[1] || [];
    const avanceCategoria = result.recordsets[2] || [];

    return { ...kpis, stockCritico, avanceCategoria };
  }

  async revertAttendance(eventoId: number, carnet: string) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('eventoId', sql.Int, eventoId);
    request.input('carnet', sql.VarChar(50), carnet);
    const result = await request.query(`
      DELETE FROM dbo.tblAsistenciaEventos
      OUTPUT DELETED.*
      WHERE EventoId = @eventoId AND CarnetColaborador = @carnet
    `);
    if (!result.recordset[0]) {
      throw new NotFoundException('No se encontró registro de asistencia para este colaborador.');
    }
    return { success: true, message: 'Asistencia revertida' };
  }

  async getCenso(eventoId: number, busqueda?: string, estado?: string, pagina = 1, porPagina = 50) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('eventoId', sql.Int, eventoId);

    let where = 'WHERE c.Activo = 1';
    if (busqueda) {
      request.input('busqueda', sql.VarChar(250), `%${busqueda}%`);
      where += ` AND (c.Carnet LIKE @busqueda OR c.Nombre LIKE @busqueda OR h.NombreHijo LIKE @busqueda)`;
    }

    let having = '';
    if (estado === 'pendientes') {
      having = 'HAVING COUNT(CASE WHEN e.Estado = \'DELIVERED\' THEN 1 END) = 0';
    } else if (estado === 'completos') {
      where += ' AND EXISTS (SELECT 1 FROM dbo.tblAsistenciaEventos ae WHERE ae.CarnetColaborador = c.Carnet AND ae.EventoId = @eventoId)';
      having = 'HAVING COUNT(DISTINCT h.Id) > 0 AND COUNT(CASE WHEN e.Estado = \'DELIVERED\' THEN 1 END) = COUNT(DISTINCT h.Id)';
    }

    const offset = (pagina - 1) * porPagina;

    const countSql = `
      SELECT COUNT(DISTINCT c.Carnet) AS total
      FROM dbo.tblColaboradores c
      LEFT JOIN dbo.tblHijos h ON h.CarnetColaborador = c.Carnet
      LEFT JOIN dbo.tblEntregasJuguetes e ON e.CarnetColaborador = c.Carnet AND e.EventoId = @eventoId AND e.Estado = 'DELIVERED'
      ${where}
    `;

    const countResult = await request.query(countSql);
    const total = countResult.recordset[0]?.total || 0;

    const dataSql = `
      SELECT
        c.Carnet, c.Nombre, c.Gerencia, c.Ubicacion,
        COUNT(DISTINCT h.Id) AS TotalHijos,
        COUNT(CASE WHEN e.Estado = 'DELIVERED' THEN 1 END) AS Entregados,
        COUNT(CASE WHEN a.Id IS NOT NULL THEN 1 END) AS Asistio,
        ISNULL(SUM(a.Adultos), 0) AS TotalAdultos,
        ISNULL(SUM(a.Ninos), 0) AS TotalNinos,
        MAX(a.RegistradoPor) AS RegistradoPor,
        MAX(a.AsistioPor) AS AsistioPor,
        MAX(a.NombreAsistente) AS NombreAsistente,
        MAX(a.FechaRegistro) AS FechaAsistencia
      FROM dbo.tblColaboradores c
      LEFT JOIN dbo.tblHijos h ON h.CarnetColaborador = c.Carnet AND h.Activo = 1
      LEFT JOIN dbo.tblEntregasJuguetes e ON e.HijoId = h.Id AND e.EventoId = @eventoId AND e.Estado = 'DELIVERED'
      LEFT JOIN dbo.tblAsistenciaEventos a ON a.CarnetColaborador = c.Carnet AND a.EventoId = @eventoId
      ${where}
      GROUP BY c.Carnet, c.Nombre, c.Gerencia, c.Ubicacion
      ${having}
      ORDER BY c.Nombre
      OFFSET ${offset} ROWS FETCH NEXT ${porPagina} ROWS ONLY
    `;

    const dataResult = await request.query(dataSql);

    return {
      data: dataResult.recordset,
      total,
      pagina,
      porPagina,
      totalPaginas: Math.ceil(total / porPagina),
    };
  }
}
