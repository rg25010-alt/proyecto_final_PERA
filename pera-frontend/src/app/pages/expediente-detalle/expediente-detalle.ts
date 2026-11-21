import { AsyncPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { Acceso } from '../login/acceso';
import { ConsultaDetalle, DetalleExpediente } from './consulta-detalle';

type Vista =
  | { estado: 'cargando' | 'ausente' | 'error' }
  | { estado: 'listo'; expediente: DetalleExpediente };

@Component({
  selector: 'app-expediente-detalle',
  imports: [AsyncPipe, DatePipe, RouterLink],
  templateUrl: './expediente-detalle.html',
  styleUrl: './expediente-detalle.scss',
})
export class ExpedienteDetalle implements OnInit {
  private readonly acceso = inject(Acceso);
  private readonly consulta = inject(ConsultaDetalle);
  private readonly router = inject(Router);
  private readonly ruta = inject(ActivatedRoute);

  get permitido() {
    const rol = this.acceso.perfil()?.rol;
    return rol === 'AdministracionAcademica' || rol === 'CoordinacionPERA' || rol === 'Jefatura' || rol === 'DocenteTutor';
  }

  readonly vista = this.ruta.paramMap.pipe(switchMap(parametros => {
    const carnet = (parametros.get('carnet') ?? '').trim().toUpperCase();
    if (!this.permitido || !/^[A-Z0-9]{1,20}$/.test(carnet)) return of<Vista>({ estado: 'ausente' });
    return this.consulta.porCarnet(carnet).pipe(
      map((expediente): Vista => {
        if (!expediente) return { estado: 'ausente' };
        return { estado: 'listo', expediente: {
          ...expediente,
          historial: [...expediente.historial].sort((a, b) => b.fecha.localeCompare(a.fecha)),
          notas: [...expediente.notas].sort((a, b) => b.fecha.localeCompare(a.fecha)),
        } };
      }),
      catchError((error: HttpErrorResponse) => of<Vista>({
        estado: [403, 404].includes(error.status) ? 'ausente' : 'error',
      })),
      startWith<Vista>({ estado: 'cargando' }),
    );
  }));

  ngOnInit() {
    if (!this.permitido) {
      void this.router.navigateByUrl(this.acceso.perfil() ? '/inicio' : '/login', { replaceUrl: true });
    }
  }
}

