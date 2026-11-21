import { Signal } from '@angular/core';

export interface EstudianteListado {
  carnet: string;
  nombre: string;
  carrera: string;
  departamento: string;
  cum: number;
  estadoPera: string;
}

export abstract class ListadoAutorizado {
  abstract readonly estudiantes: Signal<readonly EstudianteListado[]>;
}
