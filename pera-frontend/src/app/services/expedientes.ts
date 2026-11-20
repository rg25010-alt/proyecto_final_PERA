import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Carrera, Expediente, NuevoExpediente } from '../models/expediente';

@Injectable({ providedIn: 'root' })
export class ServicioExpedientes {
  private readonly http = inject(HttpClient);

  listarCarreras() {
    return this.http.get<Carrera[]>('/api/carreras');
  }

  registrar(datos: NuevoExpediente) {
    return this.http.post<Expediente>('/api/expedientes', datos);
  }

  buscar(carnet: string) {
    return this.http.get<Expediente>('/api/expedientes/' + encodeURIComponent(carnet.trim()));
  }
}
