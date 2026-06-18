import { Injectable, NotFoundException, BadRequestException } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';
import * as path from 'path';
import * as fs from 'fs';
import sharp from 'sharp';
import { v4 as uuidv4 } from 'uuid';
import { ConfigService } from '@nestjs/config';

@Injectable()
export class CatalogService {
  constructor(
    private db: DatabaseService,
    private config: ConfigService,
  ) {}

  async getAll() {
    const pool = this.db.getPool();
    const result = await pool.request().query(`
      SELECT Id, Categoria, Genero, NombreJuguete, Proveedor, CostoUnitario,
             StockInicial, StockActual, FotoUrl, Activo
      FROM dbo.tblCatalogoJuguetes
      WHERE Activo = 1
      ORDER BY Categoria, Genero, NombreJuguete
    `);
    return result.recordset.map((r: any) => ({
      id: r.Id,
      categoria: r.Categoria,
      genero: r.Genero,
      nombreJuguete: r.NombreJuguete,
      proveedor: r.Proveedor,
      costoUnitario: r.CostoUnitario,
      stockInicial: r.StockInicial,
      stockActual: r.StockActual,
      fotoUrl: r.FotoUrl,
      activo: r.Activo,
    }));
  }

  async getSummary() {
    const pool = this.db.getPool();
    const result = await pool.request().query('SELECT * FROM dbo.vw_ResumenInventario');
    return result.recordset.map((r: any) => ({
      jugueteId: r.JugueteId,
      categoria: r.Categoria,
      genero: r.Genero,
      nombreJuguete: r.NombreJuguete,
      stockInicial: r.StockInicial,
      stockActual: r.StockActual,
      entregados: r.Entregados,
      porcentajeDespacho: r.PorcentajeDespacho,
    }));
  }

  async create(dto: { categoria: string; genero: string; nombreJuguete: string; stockInicial: number }, fotoFile?: Express.Multer.File) {
    const pool = this.db.getPool();

    let fotoUrl: string | null = null;

    if (fotoFile) {
      fotoUrl = await this.savePhoto(fotoFile, 'fotos_juguetes');
    }

    const request = pool.request();
    request.input('categoria', sql.VarChar(100), dto.categoria);
    request.input('genero', sql.VarChar(10), dto.genero);
    request.input('nombreJuguete', sql.VarChar(250), dto.nombreJuguete);
    request.input('stockInicial', sql.Int, dto.stockInicial);
    request.input('stockActual', sql.Int, dto.stockInicial);
    request.input('fotoUrl', sql.VarChar(500), fotoUrl);

    try {
      const result = await request.query(`
        INSERT INTO dbo.tblCatalogoJuguetes (Categoria, Genero, NombreJuguete, StockInicial, StockActual, FotoUrl)
        OUTPUT INSERTED.*
        VALUES (@categoria, @genero, @nombreJuguete, @stockInicial, @stockActual, @fotoUrl)
      `);
      const r = result.recordset[0];
      return { ...r, id: r.Id, categoria: r.Categoria, genero: r.Genero, nombreJuguete: r.NombreJuguete,
        stockInicial: r.StockInicial, stockActual: r.StockActual, fotoUrl: r.FotoUrl, activo: r.Activo };
    } catch (err: any) {
      if (err.message?.includes('2627') || err.message?.includes('2601')) {
        throw new BadRequestException('Ya existe un juguete con esa categoria y genero.');
      }
      throw err;
    }
  }

  async update(id: number, dto: any, fotoFile?: Express.Multer.File) {
    const pool = this.db.getPool();

    const existing = await pool.request()
      .input('id', sql.Int, id)
      .query('SELECT * FROM dbo.tblCatalogoJuguetes WHERE Id = @id');

    if (!existing.recordset[0]) {
      throw new NotFoundException('Juguete no encontrado.');
    }

    let fotoUrl = existing.recordset[0].FotoUrl;
    if (fotoFile) {
      fotoUrl = await this.savePhoto(fotoFile, 'fotos_juguetes');
    }

    const setClauses: string[] = [];
    if (dto.categoria !== undefined) setClauses.push('Categoria = @categoria');
    if (dto.genero !== undefined) setClauses.push('Genero = @genero');
    if (dto.nombreJuguete !== undefined) setClauses.push('NombreJuguete = @nombreJuguete');
    if (dto.stockInicial !== undefined) {
      setClauses.push('StockInicial = @stockInicial');
      setClauses.push('StockActual = @stockInicial');
    }
    if (fotoFile) setClauses.push('FotoUrl = @fotoUrl');

    if (setClauses.length === 0) {
      throw new BadRequestException('No hay campos para actualizar.');
    }

    const request = pool.request();
    request.input('id', sql.Int, id);
    if (dto.categoria !== undefined) request.input('categoria', sql.VarChar(100), dto.categoria);
    if (dto.genero !== undefined) request.input('genero', sql.VarChar(10), dto.genero);
    if (dto.nombreJuguete !== undefined) request.input('nombreJuguete', sql.VarChar(250), dto.nombreJuguete);
    if (dto.stockInicial !== undefined) request.input('stockInicial', sql.Int, dto.stockInicial);
    if (fotoFile) request.input('fotoUrl', sql.VarChar(500), fotoUrl);

    const result = await request.query(`
      UPDATE dbo.tblCatalogoJuguetes
      SET ${setClauses.join(', ')}
      OUTPUT INSERTED.*
      WHERE Id = @id
    `);

    const r = result.recordset[0];
    return {
      id: r.Id, categoria: r.Categoria, genero: r.Genero,
      nombreJuguete: r.NombreJuguete, proveedor: r.Proveedor,
      costoUnitario: r.CostoUnitario, stockInicial: r.StockInicial,
      stockActual: r.StockActual, fotoUrl: r.FotoUrl, activo: r.Activo,
    };
  }

  async deactivate(id: number) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('id', sql.Int, id);

    const result = await request.query(`
      UPDATE dbo.tblCatalogoJuguetes
      SET Activo = 0
      OUTPUT INSERTED.*
      WHERE Id = @id
    `);

    if (!result.recordset[0]) {
      throw new NotFoundException('Juguete no encontrado.');
    }

    const r = result.recordset[0];
    return {
      id: r.Id, categoria: r.Categoria, genero: r.Genero,
      nombreJuguete: r.NombreJuguete, proveedor: r.Proveedor,
      costoUnitario: r.CostoUnitario, stockInicial: r.StockInicial,
      stockActual: r.StockActual, fotoUrl: r.FotoUrl, activo: r.Activo,
    };
  }

  async uploadPhoto(id: number, fotoFile: Express.Multer.File) {
    const pool = this.db.getPool();
    const existing = await pool.request()
      .input('id', sql.Int, id)
      .query('SELECT Id FROM dbo.tblCatalogoJuguetes WHERE Id = @id');

    if (!existing.recordset[0]) {
      throw new NotFoundException('Juguete no encontrado.');
    }

    const fotoUrl = await this.savePhoto(fotoFile, 'fotos_juguetes');

    const request = pool.request();
    request.input('id', sql.Int, id);
    request.input('fotoUrl', sql.VarChar(500), fotoUrl);

    await request.query(`
      UPDATE dbo.tblCatalogoJuguetes SET FotoUrl = @fotoUrl WHERE Id = @id
    `);

    return { fotoUrl };
  }

  private async savePhoto(file: Express.Multer.File, subfolder: string): Promise<string> {
    const uploadPath = this.config.get<string>('UPLOAD_PATH', './uploads');
    const dir = path.join(uploadPath, subfolder);

    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }

    const fileName = `${uuidv4()}.webp`;
    const filePath = path.join(dir, fileName);

    await sharp(file.buffer)
      .resize({ width: 1024, withoutEnlargement: true })
      .webp({ quality: 80 })
      .toFile(filePath);

    return `/uploads/${subfolder}/${fileName}`;
  }
}
