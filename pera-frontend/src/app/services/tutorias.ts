import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AsignacionTutor, Tutor } from '../models/tutoria';

@Injectable({ providedIn: 'root' })
export class ServicioTutorias {
  private readonly http = inject(HttpClient);
  private token = '';

  get tieneAccesoLocal() {
    return this.token.length > 0;
  }

  usarCredencial(valor: string) {
    this.token = valor.replace(/\s/g, '');
  }

  cerrarAcceso() {
    this.token = '';
  }

  private opciones() {
    return { headers: { Authorization: 'Bearer ' + this.token } };
  }

  private ruta(carnet: string) {
    return '/api/expedientes/' + encodeURIComponent(carnet) + '/tutores';
  }

  consultar(carnet: string) {
    return forkJoin({
      tutores: this.http.get<Tutor[]>('/api/tutores', this.opciones()),
      asignaciones: this.http.get<AsignacionTutor[]>(this.ruta(carnet), this.opciones()),
    });
  }

  asignar(carnet: string, tutorId: number, areaRefuerzo: string) {
    return this.http.post<AsignacionTutor>(this.ruta(carnet), { tutorId, areaRefuerzo }, this.opciones());
  }
}
