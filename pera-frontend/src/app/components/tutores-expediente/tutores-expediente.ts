import { Component, Input, OnChanges, OnDestroy, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { Acceso } from '../../pages/login/acceso';
import { AsignacionTutor, Tutor } from '../../models/tutoria';
import { ServicioTutorias } from '../../services/tutorias';

@Component({
  selector: 'app-tutores-expediente',
  templateUrl: './tutores-expediente.html',
})
export class TutoresExpediente implements OnChanges, OnDestroy {
  @Input({ required: true }) carnet = '';
  private readonly api = inject(ServicioTutorias);
  private readonly acceso = inject(Acceso);
  private peticion?: Subscription;

  readonly permitido = computed(() => this.acceso.perfil()?.rol === 'CoordinacionPERA');
  readonly estado = signal<'acceso' | 'cargando' | 'listo' | 'error'>('acceso');
  readonly tutores = signal<Tutor[]>([]);
  readonly asignaciones = signal<AsignacionTutor[]>([]);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly aviso = signal('');

  ngOnChanges() {
    this.limpiar();
    if (this.permitido()) this.cargar();
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

  cargar() {
    if (!this.permitido()) return;
    this.estado.set('cargando');
    this.error.set('');
    this.peticion = this.api.consultar(this.carnet).subscribe({
      next: datos => {
        this.tutores.set(datos.tutores);
        this.asignaciones.set(datos.asignaciones);
        this.estado.set('listo');
      },
      error: (e: unknown) => {
        this.estado.set('error');
        this.error.set(e instanceof Error ? e.message : 'No se pudieron cargar los tutores del expediente.');
      },
    });
  }

  asignar(evento: Event, formulario: HTMLFormElement) {
    evento.preventDefault();
    if (this.guardando() || !formulario.reportValidity()) return;
    const datos = new FormData(formulario);
    const area = String(datos.get('areaRefuerzo')).trim();
    const periodo = String(datos.get('periodoAcademico')).trim();
    this.error.set('');
    this.aviso.set('');
    if (!area || !periodo) {
      this.error.set('Indicá el período académico y el área de refuerzo.');
      return;
    }
    this.guardando.set(true);
    this.peticion = this.api.asignar(
      this.carnet, Number(datos.get('tutorId')), area, periodo,
    ).subscribe({
      next: asignacion => {
        this.asignaciones.update(actuales => [...actuales, asignacion]);
        this.guardando.set(false);
        formulario.reset();
        this.aviso.set('Se asignó a ' + asignacion.tutor.nombre + ' para ' + asignacion.areaRefuerzo + '.');
      },
      error: (e: unknown) => {
        this.guardando.set(false);
        this.error.set(e instanceof Error ? e.message : 'No se pudo guardar la asignación.');
      },
    });
  }
}
