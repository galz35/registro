export interface Colaborador {
  carnet: string;
  nombre: string;
  gerencia: string | null;
  ubicacion: string | null;
  puesto?: string | null;
  edificio?: string | null;
  departamentoGeografico?: string | null;
}

export interface Hijo {
  id: number;
  nombreHijo: string;
  edadHijo: number;
  generoHijo: string;
  categoria: string;
  estadoEntrega: string | null;
  entregaId: number | null;
  fechaEntrega: string | null;
  recibidoPor: string | null;
  fotoEvidenciaUrl: string | null;
  jugueteSugerido: Juguete | null;
}

export interface Juguete {
  id: number;
  categoria: string;
  genero: string;
  nombreJuguete: string;
  proveedor?: string | null;
  costoUnitario?: number | null;
  stockInicial: number;
  stockActual: number;
  fotoUrl: string | null;
  activo: boolean;
}

export interface LoginResponse {
  access_token: string;
  user: {
    carnet: string;
    nombre: string;
    correo: string;
    rol: string;
  };
}

export interface ColaboradorFicha {
  colaborador: Colaborador;
  asistio: boolean;
  fechaAsistencia: string | null;
  adultos?: number;
  ninos?: number;
  asistioPor?: string | null;
  nombreAsistente?: string | null;
  fotoHcm?: string | null;
  hijos: Hijo[];
  familiaresHcm?: { nombre: string; tipoRela: string; edad: number }[];
}

export interface DashboardKPI {
  TotalNinos: number;
  TotalColaboradores: number;
  Asistidos: number;
  Entregados: number;
  Reversados: number;
  Pendientes: number;
  PorcentajeAvance: number;
  stockCritico: Juguete[];
  avanceCategoria: { Categoria: string; Total: number; Entregados: number }[];
}

export interface CensoItem {
  Carnet: string;
  Nombre: string;
  Gerencia: string | null;
  Ubicacion: string | null;
  TotalHijos: number;
  Entregados: number;
  Asistio: number;
  TotalAdultos?: number;
  TotalNinos?: number;
  RegistradoPor?: string;
  FechaAsistencia?: string;
}

export interface PaginatedResult<T> {
  data: T[];
  total: number;
  pagina: number;
  porPagina: number;
  totalPaginas: number;
}

export interface ValidateResponse {
  stockDisponible: boolean;
  stockActual: number;
  hijoYaEntregado: boolean;
  colaboradorAsistio: boolean;
  esValido: boolean;
}
