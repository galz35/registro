import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';
import * as XLSX from 'xlsx';

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

  async generateAsistenciaExcel(eventoId: number): Promise<Buffer> {
    const pool = this.db.getPool();
    const censo = await pool.request()
      .input('eventoId', sql.Int, eventoId)
      .query(`
        SELECT c.Carnet, c.Nombre, c.Gerencia, c.Ubicacion, c.DepartamentoGeografico,
               COUNT(DISTINCT h.Id) AS TotalHijos,
               ISNULL(SUM(a.Adultos), 0) AS Adultos,
               ISNULL(SUM(a.Ninos), 0) AS Ninos,
               MAX(a.AsistioPor) AS AsistioPor,
               MAX(a.NombreAsistente) AS NombreAsistente,
               MAX(a.FechaRegistro) AS FechaAsistencia
        FROM dbo.tblColaboradores c
        LEFT JOIN dbo.tblHijos h ON h.CarnetColaborador = c.Carnet AND h.Activo = 1
        LEFT JOIN dbo.tblAsistenciaEventos a ON a.CarnetColaborador = c.Carnet AND a.EventoId = @eventoId
        GROUP BY c.Carnet, c.Nombre, c.Gerencia, c.Ubicacion, c.DepartamentoGeografico
        ORDER BY c.Nombre
      `);

    const wb = XLSX.utils.book_new();
    const rows = censo.recordset.map(r => [
      r.Carnet, r.Nombre, r.Gerencia || '', r.Ubicacion || '', r.DepartamentoGeografico || '',
      r.TotalHijos || 0, r.Adultos || 0, r.Ninos || 0,
      r.AsistioPor || 'NO ASISTIO',
      r.FechaAsistencia ? new Date(r.FechaAsistencia).toLocaleString() : '',
    ]);
    const ws = XLSX.utils.aoa_to_sheet([
      ['Carnet', 'Nombre', 'Gerencia', 'Ubicacion', 'Departamento', 'Hijos', 'Adultos', 'Niños', 'Asistió', 'Fecha'],
      ...rows,
    ]);
    ws['!cols'] = [{ wch: 12 }, { wch: 35 }, { wch: 30 }, { wch: 25 }, { wch: 18 }, { wch: 8 }, { wch: 8 }, { wch: 8 }, { wch: 15 }, { wch: 20 }];
    XLSX.utils.book_append_sheet(wb, ws, 'Asistencia');
    return Buffer.from(XLSX.write(wb, { type: 'buffer', bookType: 'xlsx' }));
  }

  async generateDespachoExcel(eventoId: number): Promise<Buffer> {
    const pool = this.db.getPool();
    const result = await pool.request()
      .input('eventoId', sql.Int, eventoId)
      .query(`
        SELECT col.Carnet, col.Nombre AS Colaborador, col.Gerencia, col.DepartamentoGeografico,
               h.NombreHijo AS Hijo, h.EdadHijo, h.Categoria,
               j.NombreJuguete AS Juguete,
               e.Estado, e.RecibidoPor, COALESCE(e.NombreReceptor, col.Nombre) AS Receptor,
               e.FechaEntrega, e.UsuarioDespacho
        FROM dbo.tblEntregasJuguetes e
        INNER JOIN dbo.tblHijos h ON e.HijoId = h.Id
        INNER JOIN dbo.tblColaboradores col ON e.CarnetColaborador = col.Carnet
        INNER JOIN dbo.tblCatalogoJuguetes j ON e.JugueteId = j.Id
        WHERE e.EventoId = @eventoId
        ORDER BY e.FechaEntrega DESC
      `);

    const wb = XLSX.utils.book_new();
    const rows = result.recordset.map(r => [
      r.Carnet, r.Colaborador, r.Gerencia || '', r.DepartamentoGeografico || '',
      r.Hijo, r.EdadHijo, r.Categoria,
      r.Juguete, r.Estado === 'DELIVERED' ? 'Entregado' : 'Reversado',
      r.Receptor, r.RecibidoPor,
      r.FechaEntrega ? new Date(r.FechaEntrega).toLocaleString() : '',
      r.UsuarioDespacho,
    ]);
    const ws = XLSX.utils.aoa_to_sheet([
      ['Carnet', 'Colaborador', 'Gerencia', 'Departamento', 'Hijo', 'Edad', 'Categoria',
       'Juguete', 'Estado', 'Receptor', 'RecibidoPor', 'Fecha', 'Despachó'],
      ...rows,
    ]);
    ws['!cols'] = [{ wch: 12 }, { wch: 35 }, { wch: 25 }, { wch: 18 }, { wch: 30 }, { wch: 8 }, { wch: 15 },
                   { wch: 30 }, { wch: 12 }, { wch: 30 }, { wch: 14 }, { wch: 20 }, { wch: 14 }];
    XLSX.utils.book_append_sheet(wb, ws, 'Despacho');
    return Buffer.from(XLSX.write(wb, { type: 'buffer', bookType: 'xlsx' }));
  }

  async generateInventarioExcel(): Promise<Buffer> {
    const pool = this.db.getPool();
    const result = await pool.request().query('SELECT * FROM dbo.vw_ResumenInventario');

    const wb = XLSX.utils.book_new();
    if (result.recordset.length > 0) {
      const rows = result.recordset.map(r => [
        r.NombreJuguete, r.Categoria, r.Genero === 'M' ? 'Niños' : r.Genero === 'F' ? 'Niñas' : 'Unisex',
        r.StockInicial, r.StockActual, r.Entregados || 0, r.Reversados || 0, r.DiferenciaStock || 0,
        r.PorcentajeDespacho || 0,
      ]);
      const ws = XLSX.utils.aoa_to_sheet([
        ['Juguete', 'Categoria', 'Genero', 'Stock Inicial', 'Stock Actual', 'Entregados', 'Reversados', 'Diferencia', '% Despacho'],
        ...rows,
      ]);
      ws['!cols'] = [{ wch: 35 }, { wch: 18 }, { wch: 10 }, { wch: 14 }, { wch: 14 }, { wch: 12 }, { wch: 12 }, { wch: 12 }, { wch: 12 }];
      XLSX.utils.book_append_sheet(wb, ws, 'Inventario');

      const entregas = await pool.request().query(`
        SELECT j.NombreJuguete, col.Carnet, col.Nombre AS Colaborador, col.DepartamentoGeografico,
               h.NombreHijo AS Hijo, e.FechaEntrega, e.UsuarioDespacho
        FROM dbo.tblEntregasJuguetes e
        INNER JOIN dbo.tblHijos h ON e.HijoId = h.Id
        INNER JOIN dbo.tblColaboradores col ON e.CarnetColaborador = col.Carnet
        INNER JOIN dbo.tblCatalogoJuguetes j ON e.JugueteId = j.Id
        WHERE e.Estado = 'DELIVERED'
        ORDER BY j.NombreJuguete, e.FechaEntrega
      `);
      if (entregas.recordset.length > 0) {
        const detRows = entregas.recordset.map(r => [
          r.NombreJuguete, r.Carnet, r.Colaborador, r.DepartamentoGeografico || '',
          r.Hijo, r.FechaEntrega ? new Date(r.FechaEntrega).toLocaleString() : '', r.UsuarioDespacho,
        ]);
        const ws2 = XLSX.utils.aoa_to_sheet([
          ['Juguete', 'Carnet', 'Colaborador', 'Departamento', 'Hijo', 'Fecha', 'Despachó'],
          ...detRows,
        ]);
        ws2['!cols'] = [{ wch: 35 }, { wch: 12 }, { wch: 35 }, { wch: 18 }, { wch: 30 }, { wch: 20 }, { wch: 14 }];
        XLSX.utils.book_append_sheet(wb, ws2, 'Detalle Entregas');
      }
    }
    return Buffer.from(XLSX.write(wb, { type: 'buffer', bookType: 'xlsx' }));
  }
}
