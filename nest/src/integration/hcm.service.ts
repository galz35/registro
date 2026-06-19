import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { HttpService } from '@nestjs/axios';
import { firstValueFrom } from 'rxjs';
import { DatabaseService } from '../database/database.service';
import * as sql from 'mssql';

@Injectable()
export class HcmService {
  constructor(
    private config: ConfigService,
    private http: HttpService,
    private db: DatabaseService,
  ) {}

  private padCarnet(carnet: string): string {
    const c = carnet.trim();
    if (/^\d+$/.test(c) && c.length < 6) return c.padStart(6, '0');
    return c;
  }

  async obtenerFotoEmpleado(carnet: string): Promise<string | null> {
    const url = this.config.get<string>('HCM_API_URL');
    const username = this.config.get<string>('HCM_USERNAME');
    const password = this.config.get<string>('HCM_PASSWORD');

    if (!url || !username || !password) return null;

    const carnetPad = this.padCarnet(carnet);

    try {
      const authHeader = 'Basic ' + Buffer.from(`${username}:${password}`).toString('base64');
      const response = await firstValueFrom(
        this.http.get(url, {
          headers: { Authorization: authHeader, Accept: 'application/json' },
          params: { onlyData: true, expand: 'photos', q: `PersonNumber='${carnetPad}'` },
          timeout: 4000,
        }),
      );
      const data = response.data;
      if (!data.items || data.items.length === 0) return null;
      const photos = data.items[0].photos;
      if (!photos || photos.length === 0) return null;
      const foto = photos.find((p: any) => p.PrimaryFlag === true) || photos[0];
      if (!foto?.Photo) return null;
      return `data:image/jpeg;base64,${foto.Photo}`;
    } catch { return null; }
  }

  async obtenerEstadoEmpleado(carnet: string): Promise<{ activo: boolean; terminationDate: string | null } | null> {
    const url = this.config.get<string>('HCM_API_URL');
    const username = this.config.get<string>('HCM_USERNAME');
    const password = this.config.get<string>('HCM_PASSWORD');

    if (!url || !username || !password) return null;

    const carnetPad = this.padCarnet(carnet);

    try {
      const authHeader = 'Basic ' + Buffer.from(`${username}:${password}`).toString('base64');
      const response = await firstValueFrom(
        this.http.get(url, {
          headers: { Authorization: authHeader, Accept: 'application/json' },
          params: { onlyData: true, expand: 'workRelationships', q: `PersonNumber='${carnetPad}'` },
          timeout: 5000,
        }),
      );
      const data = response.data;
      if (!data.items || data.items.length === 0) return null;

      const wr = data.items[0].workRelationships;
      if (!wr || wr.length === 0) return { activo: true, terminationDate: null };

      const primary = wr.find((w: any) => w.PrimaryFlag === true) || wr[0];
      const terminationDate = primary.TerminationDate || null;
      const activo = !terminationDate;

      return { activo, terminationDate };
    } catch { return null; }
  }

  async obtenerFamiliares(carnet: string): Promise<any[]> {
    const username = this.config.get<string>('HCM_USERNAME');
    const password = this.config.get<string>('HCM_PASSWORD');
    const baseUrl = 'https://fa-exjp-saasfaprod1.fa.ocs.oraclecloud.com/hcmRestApi/resources/11.13.18.05/hcmContacts';

    if (!username || !password) return [];

    const carnetPad = this.padCarnet(carnet);

    try {
      const authHeader = 'Basic ' + Buffer.from(`${username}:${password}`).toString('base64');
      const q = `contactRelationships.RelatedPersonNumber='${carnetPad}'`;
      const url = `${baseUrl}?q=${encodeURIComponent(q)}&expand=names,contactRelationships&onlyData=true`;
      const response = await firstValueFrom(
        this.http.get(url, {
          headers: { Authorization: authHeader, Accept: 'application/json', 'REST-Framework-Version': '2' },
          timeout: 8000,
        }),
      );
      const data = response.data;
      if (!data.items || data.items.length === 0) { return []; }

      return data.items.map((item: any) => {
        const nombre = item.names?.[0]?.FullName?.trim() || '';
        const rel = item.contactRelationships?.[0];
        const tipoRela = this.mapearTipoContacto(rel?.ContactType || '');
        const edad = this.calcularEdad(item.DateOfBirth);
        return { nombre, tipoRela, edad, fechaNacimiento: item.DateOfBirth || null };
      }).filter((f: any) => f.nombre && f.tipoRela && f.tipoRela !== 'CONTACTO');
    } catch (err: any) { console.error(`HCM familiares error for ${carnet}:`, err.message); return []; }
  }

  private mapearTipoContacto(tipo: string): string {
    const mapa: Record<string, string> = {
      'S': 'CONYUGE',
      'C': 'HIJO/HIJA',
      'IN_MR': 'MADRE',
      'IN_FR': 'PADRE',
      'ORA_HRX_STB_UNION': 'PAREJA CON DECLARACIÓN DE UNIÓN ESTABLE',
      'BROTHER': 'HERMANO',
      'SISTER': 'HERMANA',
      'NEPHEW': 'SOBRINO',
      'M': 'CONTACTO',
      'F': 'AMIGO/AMIGA',
      'T': 'HIJASTRO/HIJASTRA',
      'ORA_HRX_AUNT': 'TÍA',
      'ORA_HRX_OR': 'OTRO FAMILIAR',
      'ORA_HRX_UNCLE': 'TÍO',
      'ORA_HRX_SP': 'PADRASTRO/MADRASTRA',
      'NIECE': 'SOBRINA',
      'ORA_HRX_GRANDDAUGHTER': 'NIETA',
      'GC': 'NIETO/NIETA',
      'CH_GRANDFATHER': 'ABUELO',
    };
    const upper = tipo.toUpperCase().trim();
    return mapa[upper] || tipo;
  }

  private calcularEdad(fecha: string): number {
    if (!fecha) return 0;
    const dob = new Date(fecha);
    if (isNaN(dob.getTime())) return 0;
    const hoy = new Date();
    let edad = hoy.getFullYear() - dob.getFullYear();
    const mes = hoy.getMonth() - dob.getMonth();
    if (mes < 0 || (mes === 0 && hoy.getDate() < dob.getDate())) edad--;
    return Math.max(0, edad);
  }
}
