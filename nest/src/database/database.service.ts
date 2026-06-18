import { Injectable, OnModuleDestroy, OnModuleInit } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import * as sql from 'mssql';

@Injectable()
export class DatabaseService implements OnModuleInit, OnModuleDestroy {
  private pool: sql.ConnectionPool;

  constructor(private config: ConfigService) {}

  async onModuleInit() {
    try {
      await this.connect();
    } catch (err) {
      console.error('BD no disponible al iniciar. La app corriendo en modo limitado.');
    }
  }

  async onModuleDestroy() {
    if (this.pool) {
      await this.pool.close();
    }
  }

  private async connect() {
    this.pool = new sql.ConnectionPool({
      server: this.config.get<string>('DB_SERVER', 'localhost'),
      port: Number(this.config.get<string>('DB_PORT', '1433')),
      user: this.config.get<string>('DB_USER', 'sa'),
      password: this.config.get<string>('DB_PASSWORD', ''),
      database: this.config.get<string>('DB_DATABASE', 'Asistencia'),
      options: {
        encrypt: this.config.get<string>('DB_ENCRYPT', 'false') === 'true',
        trustServerCertificate: this.config.get<string>('DB_TRUST_SERVER_CERTIFICATE', 'true') === 'true',
      },
      pool: {
        max: 10,
        min: 1,
        idleTimeoutMillis: 30000,
      },
      connectionTimeout: 15000,
      requestTimeout: 15000,
    });

    try {
      await this.pool.connect();
      console.log('Conectado a SQL Server');
    } catch (err) {
      console.error('Error conectando a SQL Server:', err.message);
      throw err;
    }
  }

  isConnected(): boolean {
    return !!this.pool?.connected;
  }

  getPool(): sql.ConnectionPool {
    if (!this.pool?.connected) {
      throw new Error('Base de datos no disponible.');
    }
    return this.pool;
  }

  getRequest(): sql.Request {
    if (!this.pool?.connected) {
      throw new Error('Base de datos no disponible.');
    }
    return this.pool.request();
  }

  async healthCheck(): Promise<{ ok: boolean; error?: string }> {
    try {
      if (!this.pool?.connected) {
        return { ok: false, error: 'No conectado' };
      }
      await this.pool.request().query('SELECT 1 AS ok');
      return { ok: true };
    } catch (err: any) {
      return { ok: false, error: err.message };
    }
  }
}
