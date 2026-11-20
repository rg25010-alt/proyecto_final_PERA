import { HttpErrorResponse } from '@angular/common/http';
import { Component, Input, OnChanges, OnDestroy, inject, isDevMode, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { AsignacionTutor, Tutor } from '../../models/tutoria';
import { ServicioTutorias } from '../../services/tutorias';

@Component({
  selector: 'app-tutores-expediente',
  templateUrl: './tutores-expediente.html',
})
export class TutoresExpediente implements OnChanges, OnDestroy {
  @Input({ required: true }) carnet = '';
  private readonly api = inject(ServicioTutorias);
  private peticion?: Subscription;
  readonly desarrollo = isDevMode();
  readonly estado = signal<'acceso' | 'cargando' | 'listo' | 'error'>('acceso');
  readonly tutores = signal<Tutor[]>([]);
  readonly asignaciones = signal<AsignacionTutor[]>([]);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly aviso = signal('');

  ngOnChanges() {
    this.limpiar();
    if (this.api.tieneAccesoLocal) this.cargar();
  }

  ngOnDestroy() {
    this.peticion?.unsubscribe();
  }

  private limpiar() {
    this.peticion?.unsubscribe();
    this.tutores.set([]);
    this.asignaciones.set([]);
    this.aviso.set('');
    this.error.set('');
    this.guardando.set(false);
    this.estado.set('acceso');
  }

  cerrar() {
    this.api.cerrarAcceso();
    this.limpiar();
  }

  acceder(evento: Event, campo: HTMLInputElement) {
    evento.preventDefault();
    this.api.usarCredencial(campo.value);
    campo.value = '';
    if (this.api.tieneAccesoLocal) this.cargar();
    else this.error.set('Ingresá la credencial de Coordinación.');
  }

  cargar() {
    this.estado.set('cargando');
    this.error.set('');
    this.peticion = this.api.consultar(this.carnet).subscribe({
      next: datos => {
        this.tutores.set(datos.tutores);
        this.asignaciones.set(datos.asignaciones);
        this.estado.set('listo');
      },
      error: (e: HttpErrorResponse) => {
        this.estado.set('error');
        this.mostrarError(e, 'No se pudieron cargar los tutores del expediente.');
      },
    });
  }

  asignar(evento: Event, formulario: HTMLFormElement) {
    evento.preventDefault();
    if (this.guardando() || !formulario.reportValidity()) return;
    const datos = new FormData(formulario);
    const area = String(datos.get('areaRefuerzo')).trim();
    this.error.set('');
    this.aviso.set('');
    if (!area) {
      this.error.set('Indicá el área de refuerzo.');
      return;
    }
    this.guardando.set(true);
    this.peticion = this.api.asignar(this.carnet, Number(datos.get('tutorId')), area).subscribe({
      next: asignacion => {
        this.asignaciones.update(actuales => [...actuales, asignacion]);
        this.guardando.set(false);
        formulario.reset();
        this.aviso.set('Se asignó a ' + asignacion.tutor.nombre + ' para ' + asignacion.areaRefuerzo + '.');
      },
      error: (e: HttpErrorResponse) => {
        this.guardando.set(false);
        this.mostrarError(e, 'No se pudo guardar la asignación.');
      },
    });
  }

  private mostrarError(e: HttpErrorResponse, mensaje: string) {
    if (e.status === 401 || e.status === 403) {
      this.cerrar();
      this.error.set(e.status === 401
        ? 'La credencial venció o no es válida. Ingresá la credencial del backend en ejecución.'
        : 'Se necesita el rol de Coordinación PERA para acceder.');
      return;
    }
    this.error.set(e.error?.mensaje || mensaje);
  }
}
