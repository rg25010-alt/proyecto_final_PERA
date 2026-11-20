export interface Tutor {
  id: number;
  nombre: string;
  departamento: string;
  activo: boolean;
}

export interface AsignacionTutor {
  id: number;
  carnet: string;
  tutor: Tutor;
  areaRefuerzo: string;
  fechaAsignacion: string;
  responsable: string;
}
