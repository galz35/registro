import { Injectable, UnauthorizedException } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { ConfigService } from '@nestjs/config';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';
import * as jwt from 'jsonwebtoken';

@Injectable()
export class AuthService {
  constructor(
    private db: DatabaseService,
    private jwtService: JwtService,
    private config: ConfigService,
  ) {}

  async ssoLogin(ssoToken: string) {
    const ssoSecret = this.config.get<string>('SSO_SECRET') || '';

    let payload: any;
    try {
      payload = jwt.verify(ssoToken, ssoSecret);
    } catch {
      throw new UnauthorizedException('Token SSO invalido o expirado.');
    }

    if (payload.type !== 'SSO_PORTAL') {
      throw new UnauthorizedException('Tipo de token SSO incorrecto.');
    }

    const carnet = payload.carnet || payload.sub;
    if (!carnet) {
      throw new UnauthorizedException('El token SSO no contiene carnet.');
    }

    const nombre = payload.name || carnet;
    const correo = payload.correo || '';

    const pool = this.db.getPool();
    const request = pool.request();
    request.input('carnet', sql.VarChar(50), carnet);

    const result = await request.query(`
      SELECT Id, Carnet, Correo, Nombre, Rol, Activo
      FROM dbo.tblUsuariosAsistencia
      WHERE Carnet = @carnet
    `);

    let user = result.recordset[0];

    if (!user) {
      const insertReq = pool.request();
      insertReq.input('carnet', sql.VarChar(50), carnet);
      insertReq.input('correo', sql.VarChar(250), correo);
      insertReq.input('nombre', sql.VarChar(250), nombre);
      insertReq.input('rol', sql.VarChar(50), 'despachador');

      const insertResult = await insertReq.query(`
        INSERT INTO dbo.tblUsuariosAsistencia (Carnet, Correo, Nombre, Rol)
        OUTPUT INSERTED.Id, INSERTED.Carnet, INSERTED.Correo, INSERTED.Nombre, INSERTED.Rol, INSERTED.Activo
        VALUES (@carnet, @correo, @nombre, @rol)
      `);

      user = insertResult.recordset[0];
    }

    if (!user.Activo) {
      throw new UnauthorizedException('Usuario inactivo en el sistema.');
    }

    const rolesMovil = ['despachador', 'supervisor', 'admin'];
    if (!rolesMovil.includes(user.Rol)) {
      throw new UnauthorizedException('Usuario sin permiso para operar Asistencia.');
    }

    const token = this.jwtService.sign({
      sub: user.Carnet,
      carnet: user.Carnet,
      nombre: user.Nombre,
      rol: user.Rol,
    });

    return {
      access_token: token,
      user: {
        carnet: user.Carnet,
        nombre: user.Nombre,
        correo: user.Correo,
        rol: user.Rol,
      },
    };
  }

  async devLogin(carnet: string) {
    if (process.env.NODE_ENV === 'production') {
      throw new UnauthorizedException('Dev login no disponible en produccion.');
    }

    const pool = this.db.getPool();
    const request = pool.request();
    request.input('carnet', sql.VarChar(50), carnet);

    const result = await request.query(`
      SELECT Id, Carnet, Correo, Nombre, Rol, Activo
      FROM dbo.tblUsuariosAsistencia
      WHERE Carnet = @carnet
    `);

    const user = result.recordset[0];
    if (!user) {
      throw new UnauthorizedException('Usuario no encontrado.');
    }

    const token = this.jwtService.sign({
      sub: user.Carnet,
      carnet: user.Carnet,
      nombre: user.Nombre,
      rol: user.Rol,
    });

    return {
      access_token: token,
      user: {
        carnet: user.Carnet,
        nombre: user.Nombre,
        correo: user.Correo,
        rol: user.Rol,
      },
    };
  }

  async getMe(carnet: string) {
    const pool = this.db.getPool();
    const request = pool.request();
    request.input('carnet', sql.VarChar(50), carnet);

    const result = await request.query(`
      SELECT Carnet, Correo, Nombre, Rol, UltimoLogin
      FROM dbo.tblUsuariosAsistencia
      WHERE Carnet = @carnet
    `);

    const r = result.recordset[0];
    if (!r) return null;
    return { carnet: r.Carnet, correo: r.Correo, nombre: r.Nombre, rol: r.Rol, ultimoLogin: r.UltimoLogin };
  }
}
