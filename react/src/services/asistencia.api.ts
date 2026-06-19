import api from './api';
import type {
  LoginResponse, ColaboradorFicha, DashboardKPI,
  PaginatedResult, CensoItem, ValidateResponse, Juguete,
} from '../types';

export async function login(carnet: string, _password: string): Promise<LoginResponse> {
  const { data } = await api.post('/auth/dev-login', { carnet, password: _password });
  return data;
}

export async function getColaboradorFull(carnet: string, eventoId: number): Promise<ColaboradorFicha> {
  const { data } = await api.get(`/attendance/lookup/${carnet}`, { params: { eventoId } });
  return data;
}

export async function registrarAsistencia(eventoId: number, carnet: string, adultos = 1, ninos = 0, asistioPor?: string, nombreAsistente?: string): Promise<any> {
  const { data } = await api.post('/attendance/register', { eventoId, carnet, adultos, ninos, asistioPor, nombreAsistente });
  return data;
}

export async function getSummary(eventoId: number): Promise<DashboardKPI> {
  const { data } = await api.get(`/attendance/event/${eventoId}/summary`);
  return data;
}

export async function getCenso(
  eventoId: number,
  busqueda?: string,
  estado?: string,
  pagina = 1,
  porPagina = 50,
): Promise<PaginatedResult<CensoItem>> {
  const { data } = await api.get('/attendance/censo', {
    params: { eventoId, busqueda, estado, pagina, porPagina },
  });
  return data;
}

export async function validateDelivery(hijoId: number, jugueteId: number, eventoId: number): Promise<ValidateResponse> {
  const { data } = await api.get(`/dispatch/validate/${hijoId}/${jugueteId}`, { params: { eventoId } });
  return data;
}

export async function registrarEntrega(formData: FormData): Promise<any> {
  const { data } = await api.post('/dispatch/deliver', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function reversarEntrega(entregaId: number, motivo: string): Promise<any> {
  const { data } = await api.post(`/dispatch/${entregaId}/revert`, { motivo });
  return data;
}

export async function getCatalogo(): Promise<Juguete[]> {
  const { data } = await api.get('/catalog');
  return data;
}

export async function createJuguete(formData: FormData): Promise<Juguete> {
  const { data } = await api.post('/catalog', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function updateJuguete(id: number, formData: FormData): Promise<Juguete> {
  const { data } = await api.put(`/catalog/${id}`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function deactivateJuguete(id: number): Promise<Juguete> {
  const { data } = await api.patch(`/catalog/${id}/deactivate`);
  return data;
}

export async function importCenso(file: File): Promise<any> {
  const formData = new FormData();
  formData.append('archivo', file);
  const { data } = await api.post('/imports/censo/apply', formData);
  return data;
}

export async function importCatalogo(file: File): Promise<any> {
  const formData = new FormData();
  formData.append('archivo', file);
  const { data } = await api.post('/imports/catalogo/apply', formData);
  return data;
}
