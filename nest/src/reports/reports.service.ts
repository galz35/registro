import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';

@Injectable()
export class ReportsService {
  constructor(private db: DatabaseService) {}

  async getEntregasCSV(eventoId: number) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('eventoId', sql.Int, eventoId);

    const result = await request.query(`
      SELECT
        col.Carnet, col.Nombre AS Colaborador, col.Gerencia,
        h.NombreHijo AS Hijo, h.EdadHijo, h.GeneroHijo, h.Categoria,
        j.NombreJuguete AS Juguete,
        e.Estado, e.RecibidoPor,
        COALESCE(e.NombreReceptor, col.Nombre) AS Receptor,
        e.FechaEntrega, e.UsuarioDespacho,
        e.FechaReversion, e.MotivoReversion
      FROM dbo.tblEntregasJuguetes e
      INNER JOIN dbo.tblHijos h ON e.HijoId = h.Id
      INNER JOIN dbo.tblColaboradores col ON e.CarnetColaborador = col.Carnet
      INNER JOIN dbo.tblCatalogoJuguetes j ON e.JugueteId = j.Id
      WHERE e.EventoId = @eventoId
      ORDER BY e.FechaEntrega DESC
    `);

    if (result.recordset.length === 0) return '';

    const headers = Object.keys(result.recordset[0]);
    const rows = result.recordset.map((row: any) =>
      headers.map((h) => {
        const val = row[h];
        if (val === null || val === undefined) return '';
        const str = String(val);
        return str.includes(',') || str.includes('"') ? `"${str.replace(/"/g, '""')}"` : str;
      }).join(','),
    );

    return [headers.join(','), ...rows].join('\n');
  }

  async getPendientesCSV(eventoId: number) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('eventoId', sql.Int, eventoId);

    const result = await request.query(`
      SELECT
        c.Carnet, c.Nombre AS Colaborador, c.Gerencia, c.Ubicacion,
        h.NombreHijo AS Hijo, h.EdadHijo, h.GeneroHijo, h.Categoria
      FROM dbo.tblHijos h
      INNER JOIN dbo.tblColaboradores c ON h.CarnetColaborador = c.Carnet
      WHERE h.Activo = 1
        AND NOT EXISTS (
          SELECT 1 FROM dbo.tblEntregasJuguetes e
          WHERE e.HijoId = h.Id AND e.EventoId = @eventoId AND e.Estado = 'DELIVERED'
        )
      ORDER BY c.Nombre, h.NombreHijo
    `);

    if (result.recordset.length === 0) return '';

    const headers = Object.keys(result.recordset[0]);
    const rows = result.recordset.map((row: any) =>
      headers.map((h) => {
        const val = row[h];
        if (val === null || val === undefined) return '';
        return String(val).includes(',') ? `"${val}"` : String(val);
      }).join(','),
    );

    return [headers.join(','), ...rows].join('\n');
  }

  async getInventarioCSV() {
    const pool = this.db.getPool();
    const result = await pool.request().query('SELECT * FROM dbo.vw_ResumenInventario');

    if (result.recordset.length === 0) return '';

    const headers = Object.keys(result.recordset[0]);
    const rows = result.recordset.map((row: any) =>
      headers.map((h) => {
        const val = row[h];
        if (val === null || val === undefined) return '';
        return String(val).includes(',') ? `"${val}"` : String(val);
      }).join(','),
    );

    return [headers.join(','), ...rows].join('\n');
  }
}
