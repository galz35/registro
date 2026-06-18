import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';

@Injectable()
export class AdminService {
  constructor(private db: DatabaseService) {}

  async searchUsers(query: string) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('q', sql.VarChar(100), `%${query}%`);
    const result = await request.query(`
      SELECT carnet, nombreCompleto AS nombre, correo, activo, cargo, orgGerencia AS gerencia, ubicacion
      FROM bdplaner.dbo.p_Usuarios
      WHERE carnet LIKE @q OR nombreCompleto LIKE @q OR correo LIKE @q
      ORDER BY carnet
      OFFSET 0 ROWS FETCH NEXT 30 ROWS ONLY
    `);
    return result.recordset.map((u: any) => ({
      carnet: u.carnet,
      nombre: u.nombre,
      correo: u.correo || '',
      activo: u.activo === 1 || u.activo === true,
      cargo: u.cargo || null,
      gerencia: u.gerencia || null,
      ubicacion: u.ubicacion || null,
    }));
  }

  async getUserRoles() {
    const pool = this.db.getPool();
    const result = await pool.request().query(`
      SELECT u.Id, u.Carnet, u.Correo, u.Nombre, u.Rol, u.Activo, u.UltimoLogin
      FROM dbo.tblUsuariosAsistencia u
      ORDER BY u.Nombre
    `);
    return result.recordset.map((u: any) => ({
      id: u.Id,
      carnet: u.Carnet,
      correo: u.Correo || '',
      nombre: u.Nombre,
      rol: u.Rol,
      activo: u.Activo === 1 || u.Activo === true,
      ultimoLogin: u.UltimoLogin || null,
    }));
  }

  async setRole(carnet: string, rol: string) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('carnet', sql.VarChar(50), carnet);
    request.input('rol', sql.VarChar(50), rol);

    // Check if user exists, update role, else create
    const exists = await request.query(`
      IF EXISTS (SELECT 1 FROM dbo.tblUsuariosAsistencia WHERE Carnet = @carnet)
        UPDATE dbo.tblUsuariosAsistencia SET Rol = @rol WHERE Carnet = @carnet
      ELSE
        INSERT INTO dbo.tblUsuariosAsistencia (Carnet, Nombre, Rol)
          SELECT @carnet, nombreCompleto, @rol FROM bdplaner.dbo.p_Usuarios WHERE carnet = @carnet
    `);
    return { success: true, carnet, rol };
  }
}
