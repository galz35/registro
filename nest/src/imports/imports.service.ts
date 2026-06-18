import { Injectable, BadRequestException } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import * as XLSX from 'xlsx';
import * as sql from 'mssql';
import * as path from 'path';
import * as fs from 'fs';
import * as crypto from 'crypto';
import * as sharp from 'sharp';
import { v4 as uuidv4 } from 'uuid';
import { ConfigService } from '@nestjs/config';

const CATEGORIAS_VALIDAS = [
  'ENTRE 0-1', 'ENTRE 01.1-2', 'ENTRE 02.1-3', 'ENTRE 03.1-4',
  'ENTRE 04.1-5', 'ENTRE 05.1-6', 'ENTRE 06.1-7', 'ENTRE 07.1-8',
  'ENTRE 08.1-9', 'ENTRE 09.1-10', 'ENTRE 10.1-11', 'ENTRE 11.1-11.99',
];

const MAPA_CATEGORIA: Record<string, string> = {
  'KENSA 0 - 1': 'ENTRE 0-1',
  'TOYS 1 - 2': 'ENTRE 01.1-2',
  'KENSA 1 - 2': 'ENTRE 01.1-2',
  'TOYS 2 - 3': 'ENTRE 02.1-3',
  'KENSA 2 - 3': 'ENTRE 02.1-3',
  'KENSA 3 - 4': 'ENTRE 03.1-4',
  'TOYS 3 - 4': 'ENTRE 03.1-4',
  'TOYS 4 - 5': 'ENTRE 04.1-5',
  'TOYS 5 - 6': 'ENTRE 05.1-6',
  'KENSA 5 - 6': 'ENTRE 05.1-6',
  'TOYS 6 - 7': 'ENTRE 06.1-7',
  'KENSA 6 - 7': 'ENTRE 06.1-7',
  'TOYS 7 - 8': 'ENTRE 07.1-8',
  'KENSA 7 - 8': 'ENTRE 07.1-8',
  'TOYS 8 - 9': 'ENTRE 08.1-9',
  'TOYS 9 - 10': 'ENTRE 09.1-10',
  'TOYS 10 - 11': 'ENTRE 10.1-11',
  'TOYS 11 - 12': 'ENTRE 11.1-11.99',
  'KENSA 11 - 12': 'ENTRE 11.1-11.99',
};

const MAPA_GENERO: Record<string, string> = {
  'NIÑOS': 'M',
  'NIÑAS': 'F',
  'UNISEX': 'TODOS',
};

@Injectable()
export class ImportsService {
  constructor(
    private db: DatabaseService,
    private config: ConfigService,
  ) {}

  // ======================================================================
  // CENSO
  // ======================================================================

  async validateCenso(file: Express.Multer.File) {
    const workbook = XLSX.read(file.buffer, { type: 'buffer' });

    if (!workbook.SheetNames.includes('DATA HIJOS')) {
      throw new BadRequestException('El archivo debe contener la hoja "DATA HIJOS".');
    }

    const sheet = workbook.Sheets['DATA HIJOS'];
    const rows: any[] = XLSX.utils.sheet_to_json(sheet, { defval: '' });

    if (rows.length === 0) {
      throw new BadRequestException('La hoja DATA HIJOS esta vacia.');
    }

    const errores: any[] = [];
    const duplicados: any[] = [];
    const seen = new Map<string, number[]>();

    for (let i = 0; i < rows.length; i++) {
      const row = rows[i];
      const fila = i + 2;

      const carnet = String(row['CARNET COLABORADOR'] || '').trim();
      const nombreHijo = String(row['NOMBRE HIJO'] || '').trim();
      const generoStr = String(row['GENERO HIJO'] || '').trim().toUpperCase();
      const categoria = String(row['CATEGORIA'] || '').trim();
      const fechaNac = row['FECHA NACIMIENTO HIJO'];

      if (!carnet) {
        errores.push({ fila, campo: 'CARNET COLABORADOR', valor: '', mensaje: 'Carnet vacio.' });
      }
      if (!nombreHijo) {
        errores.push({ fila, campo: 'NOMBRE HIJO', valor: '', mensaje: 'Nombre del hijo vacio.' });
      }
      if (!['M', 'F'].includes(generoStr)) {
        errores.push({ fila, campo: 'GENERO HIJO', valor: generoStr, mensaje: `Genero invalido: "${generoStr}". Debe ser M o F.` });
      }
      if (categoria && !CATEGORIAS_VALIDAS.includes(categoria)) {
        errores.push({ fila, campo: 'CATEGORIA', valor: categoria, mensaje: `Categoria desconocida: "${categoria}".` });
      }

      const key = `${carnet}|${nombreHijo.toUpperCase()}|${fechaNac}`;
      if (key && carnet) {
        if (seen.has(key)) {
          const prevFila = seen.get(key)?.[0] || 0;
          duplicados.push({ fila, carnet, nombreHijo, fechaNac, genero: generoStr, filaOriginal: prevFila });
        } else {
          seen.set(key, [fila]);
        }
      }
    }

    const totalHijos = rows.length;
    const carnetsUnicos = new Set(rows.map((r: any) => String(r['CARNET COLABORADOR'] || '').trim()).filter(Boolean)).size;

    return {
      status: 'validated',
      archivo: file.originalname,
      resumen: {
        hijos: totalHijos,
        colaboradores: carnetsUnicos,
        duplicados: duplicados.length,
        erroresBloqueantes: errores.length,
      },
      errores,
      duplicados,
    };
  }

  async applyCenso(file: Express.Multer.File, usuario: string) {
    const validation = await this.validateCenso(file);
    if (validation.resumen.erroresBloqueantes > 0) {
      throw new BadRequestException({
        message: `No se puede aplicar: ${validation.resumen.erroresBloqueantes} errores bloqueantes.`,
        errores: validation.errores,
      });
    }

    const fileHash = crypto.createHash('sha256').update(file.buffer).digest('hex');
    const pool = this.db.getPool();

    const workbook = XLSX.read(file.buffer, { type: 'buffer' });
    const sheet = workbook.Sheets['DATA HIJOS'];
    const rows: any[] = XLSX.utils.sheet_to_json(sheet, { defval: '' });

    const insertados = { colaboradores: 0, hijos: 0 };

    for (const row of rows) {
      const carnet = String(row['CARNET COLABORADOR'] || '').trim();
      const nombreColab = String(row['NOMBRE COMPLETO COLABORADOR'] || '').trim();
      const puesto = String(row['BDACT.PUESTO'] || '').trim();
      const gerencia = String(row['BDACT.GERENCIA'] || '').trim().replace(/^NI\s+/, '');
      const ubicacion = String(row['BDACT.UBICACION'] || '').trim().replace(/^NI\s+/, '');
      const edificio = String(row['BDACT.NOMBRE EDIFICIO'] || '').trim();
      const deptoGeo = String(row['BDACT.DEPARTAMENTO GEOGRAFICO'] || '').trim();
      const fechaContratacion = row['BDACT.FECHA CONTRATACION'] || null;
      const generoColab = String(row['BDACT.GENERO'] || '').trim();

      const nombreHijo = String(row['NOMBRE HIJO'] || '').trim();
      const fechaNac = this.parseExcelDate(row['FECHA NACIMIENTO HIJO']);
      const edad = parseFloat(row['EDAD HIJO']) || 0;
      const generoHijo = String(row['GENERO HIJO'] || '').trim().toUpperCase();
      const categoria = String(row['CATEGORIA'] || '').trim();

      const req = pool.request();
      req.input('carnet', sql.VarChar(50), carnet);
      const exists = await req.query('SELECT COUNT(1) AS cnt FROM dbo.tblColaboradores WHERE Carnet = @carnet');

      if (exists.recordset[0].cnt === 0) {
        const insertColab = pool.request();
        insertColab.input('carnet', sql.VarChar(50), carnet);
        insertColab.input('nombre', sql.VarChar(250), nombreColab);
        insertColab.input('puesto', sql.VarChar(250), puesto || null);
        insertColab.input('gerencia', sql.VarChar(250), gerencia || null);
        insertColab.input('ubicacion', sql.VarChar(250), ubicacion || null);
        insertColab.input('edificio', sql.VarChar(250), edificio || null);
        insertColab.input('fechaContratacion', sql.Date, fechaContratacion || null);
        insertColab.input('genero', sql.VarChar(10), generoColab || null);
        insertColab.input('deptoGeo', sql.VarChar(250), deptoGeo || null);

        await insertColab.query(`
          INSERT INTO dbo.tblColaboradores (Carnet, Nombre, Puesto, Gerencia, Ubicacion, Edificio, FechaContratacion, Genero, DepartamentoGeografico)
          VALUES (@carnet, @nombre, @puesto, @gerencia, @ubicacion, @edificio, @fechaContratacion, @genero, @deptoGeo)
        `);
        insertados.colaboradores++;
      }

      const insertHijo = pool.request();
      insertHijo.input('carnetColaborador', sql.VarChar(50), carnet);
      insertHijo.input('nombreHijo', sql.VarChar(250), nombreHijo);
      insertHijo.input('fechaNacimiento', sql.Date, fechaNac);
      insertHijo.input('edadHijo', sql.Decimal(18, 2), edad);
      insertHijo.input('generoHijo', sql.VarChar(10), generoHijo);
      insertHijo.input('categoria', sql.VarChar(100), categoria);

      await insertHijo.query(`
        INSERT INTO dbo.tblHijos (CarnetColaborador, NombreHijo, FechaNacimiento, EdadHijo, GeneroHijo, Categoria)
        VALUES (@carnetColaborador, @nombreHijo, @fechaNacimiento, @edadHijo, @generoHijo, @categoria)
      `);
      insertados.hijos++;
    }

    const request = pool.request();
    request.input('tipo', sql.VarChar(20), 'CENSO');
    request.input('archivoNombre', sql.VarChar(500), file.originalname);
    request.input('hashArchivo', sql.VarChar(64), fileHash);
    request.input('usuarioImporta', sql.VarChar(100), usuario);
    request.input('estado', sql.VarChar(20), 'APLICADO');
    request.input('resumenJson', sql.VarChar(sql.MAX), JSON.stringify(insertados));

    await request.query(`
      INSERT INTO dbo.tblImportBatches (Tipo, ArchivoNombre, HashArchivo, UsuarioImporta, Estado, ResumenJson)
      VALUES (@tipo, @archivoNombre, @hashArchivo, @usuarioImporta, @estado, @resumenJson)
    `);

    return { success: true, ...insertados };
  }

  // ======================================================================
  // CATALOGO
  // ======================================================================

  // Lee el catalogo por filas (col index), hereda edad vacia de fila anterior
  private parseCatalogoRows(workbook: XLSX.WorkBook): { juguetes: any[]; errores: any[]; totalUnidades: number } {
    const sheet = workbook.Sheets['IMPRIMIR'];
    const data: any[][] = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '' });

    const errores: any[] = [];
    const juguetes: any[] = [];
    let totalUnidades = 0;
    let ultimaEdad = '';

    for (let i = 0; i < data.length; i++) {
      const row = data[i];
      if (row.length < 4) continue;

      const edadCell = String(row[1] || '').trim().replace(/\s+/g, ' ');
      const codigoCell = String(row[2] || '').trim().replace(/\s+/g, ' ');
      const descCell = String(row[3] || '').trim().replace(/\s+/g, ' ');
      const cantidadStr = String(row[4] || '0').trim();

      // Saltar filas sin datos de juguete (imagenes, separadores)
      if (!codigoCell && !descCell) continue;

      // Saltar encabezados y filas de resumen
      if (codigoCell === 'CODIGO' || descCell.includes('PROPUESTA')) continue;
      if (edadCell === 'TOTAL NIÑOS' || edadCell === 'TOTAL NIÑAS' ||
          edadCell === 'JUGUETES UNIXES' || edadCell === 'TOTAL APROBADO' ||
          edadCell === 'Vo.Bo.') continue;

      // Heredar edad de la ultima fila con edad
      if (edadCell) {
        ultimaEdad = edadCell;
      }

      if (!ultimaEdad) {
        errores.push({ fila: i + 1, campo: 'EDAD', valor: '', mensaje: 'Edad vacia y no hay fila anterior con edad.' });
        continue;
      }

      const categoria = MAPA_CATEGORIA[ultimaEdad];
      if (!categoria) {
        errores.push({ fila: i + 1, campo: 'EDAD', valor: ultimaEdad, mensaje: `Edad no mapeable: "${ultimaEdad}".` });
        continue;
      }

      const genero = MAPA_GENERO[codigoCell];
      if (!genero) {
        errores.push({ fila: i + 1, campo: 'CODIGO', valor: codigoCell, mensaje: `Codigo no mapeable: "${codigoCell}".` });
        continue;
      }

      if (!descCell) {
        errores.push({ fila: i + 1, campo: 'DESCRIPCION', valor: '', mensaje: 'Descripcion vacia.' });
        continue;
      }

      const cantidad = parseInt(cantidadStr) || 0;
      const proveedor = ultimaEdad.startsWith('KENSA') ? 'KENSA' : 'TOYS';

      juguetes.push({
        categoria,
        genero,
        nombreJuguete: descCell,
        stock: cantidad,
        proveedor,
      });
      totalUnidades += cantidad;
    }

    return { juguetes, errores, totalUnidades };
  }

  async validateCatalogo(file: Express.Multer.File) {
    const workbook = XLSX.read(file.buffer, { type: 'buffer' });

    if (!workbook.SheetNames.includes('IMPRIMIR')) {
      throw new BadRequestException('El archivo debe contener la hoja "IMPRIMIR".');
    }

    const { juguetes, errores, totalUnidades } = this.parseCatalogoRows(workbook);

    return {
      status: 'validated',
      archivo: file.originalname,
      resumen: {
        juguetes: juguetes.length,
        unidades: totalUnidades,
        proveedores: [...new Set(juguetes.map((j: any) => j.proveedor))],
        erroresBloqueantes: errores.length,
      },
      errores,
      juguetes: juguetes.slice(0, 5),
    };
  }

  async applyCatalogo(file: Express.Multer.File, usuario: string) {
    const workbook = XLSX.read(file.buffer, { type: 'buffer' });
    const { juguetes, errores } = this.parseCatalogoRows(workbook);

    if (errores.length > 0) {
      throw new BadRequestException({
        message: `No se puede aplicar: ${errores.length} errores.`,
        errores,
      });
    }

    const fileHash = crypto.createHash('sha256').update(file.buffer).digest('hex');
    const pool = this.db.getPool();

    let insertados = 0;

    for (const j of juguetes) {
      const request = pool.request();
      request.input('categoria', sql.VarChar(100), j.categoria);
      request.input('genero', sql.VarChar(10), j.genero);
      request.input('nombreJuguete', sql.VarChar(250), j.nombreJuguete);
      request.input('proveedor', sql.VarChar(100), j.proveedor);
      request.input('stockInicial', sql.Int, j.stock);
      request.input('stockActual', sql.Int, j.stock);

      try {
        await request.query(`
          INSERT INTO dbo.tblCatalogoJuguetes (Categoria, Genero, NombreJuguete, Proveedor, StockInicial, StockActual)
          VALUES (@categoria, @genero, @nombreJuguete, @proveedor, @stockInicial, @stockActual)
        `);
        insertados++;
      } catch {
        // Duplicado, ignorar
      }
    }

    const request = pool.request();
    request.input('tipo', sql.VarChar(20), 'CATALOGO');
    request.input('archivoNombre', sql.VarChar(500), file.originalname);
    request.input('hashArchivo', sql.VarChar(64), fileHash);
    request.input('usuarioImporta', sql.VarChar(100), usuario);
    request.input('estado', sql.VarChar(20), 'APLICADO');
    request.input('resumenJson', sql.VarChar(sql.MAX), JSON.stringify({ juguetes: insertados }));

    await request.query(`
      INSERT INTO dbo.tblImportBatches (Tipo, ArchivoNombre, HashArchivo, UsuarioImporta, Estado, ResumenJson)
      VALUES (@tipo, @archivoNombre, @hashArchivo, @usuarioImporta, @estado, @resumenJson)
    `);

    return { success: true, juguetes: insertados };
  }

  async getErrors(batchId: number) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('id', sql.Int, batchId);

    const result = await request.query(`
      SELECT * FROM dbo.tblImportErrores WHERE BatchId = @id
    `);

    return result.recordset.map((r: any) => ({
      id: r.Id, batchId: r.BatchId, hoja: r.Hoja,
      fila: r.Fila, campo: r.Campo, valor: r.Valor, mensaje: r.Mensaje,
    }));
  }

  private parseExcelDate(value: any): Date | null {
    if (!value) return null;

    if (typeof value === 'number') {
      const excelEpoch = new Date(1899, 11, 30);
      return new Date(excelEpoch.getTime() + value * 86400000);
    }

    if (typeof value === 'string') {
      const parts = value.split('/');
      if (parts.length === 3) {
        return new Date(parseInt(parts[2]), parseInt(parts[1]) - 1, parseInt(parts[0]));
      }
      return new Date(value);
    }

    if (value instanceof Date) return value;
    return null;
  }
}
