export interface Carrera {
  id: number;
  nombre: string;
}

export interface NuevoExpediente {
  carnet: string;
  nombre: string;
  carreraId: number;
  cum: number;
  apellidos?: string;
  correo?: string;
  clave?: string;
  correoContacto?: string | null;
}

export interface Expediente extends NuevoExpediente {
  estadoPera: string;
  topeUv: number | null;
}
