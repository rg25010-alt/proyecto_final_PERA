import { Observable } from 'rxjs';
import { Tutor } from '../../models/tutoria';
import { EstudianteListado } from '../estudiantes/listado-autorizado';

export interface TutorDelExpediente {
  tutor: Pick<Tutor, 'id' | 'nombre'>;
  areaRefuerzo: string;
}

export interface SesionDelHistorial {
  id: number;
  fecha: string;
  tutor: string;
  asistencia: string;
  tema: string;
  acuerdos: string;
}

export interface NotaParcial {
  id: number;
  fecha: string;
  actividad: string;
  nota: number;
}

export interface DetalleExpediente extends EstudianteListado {
  topeUv: number | null;
  tutoresAsignados: TutorDelExpediente[];
  historial: SesionDelHistorial[];
  notas: NotaParcial[];
}

export abstract class ConsultaDetalle {
  abstract porCarnet(carnet: string): Observable<DetalleExpediente | null>;
}
