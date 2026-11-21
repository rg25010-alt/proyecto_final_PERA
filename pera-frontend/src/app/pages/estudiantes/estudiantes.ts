import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Acceso } from '../login/acceso';
import { EstudianteListado, ListadoAutorizado } from './listado-autorizado';

@Component({
  selector: 'app-estudiantes',
  imports: [RouterLink],
  templateUrl: './estudiantes.html',
  styleUrl: './estudiantes.scss',
})
export class Estudiantes implements OnInit {
  private readonly acceso = inject(Acceso);
  private readonly router = inject(Router);
  readonly catalogo = inject(ListadoAutorizado);
  readonly permitido = computed(() => {
    const rol = this.acceso.perfil()?.rol;
    return rol === 'AdministracionAcademica' || rol === 'CoordinacionPERA' ||
      rol === 'Jefatura' || rol === 'DocenteTutor';
  });
  readonly carnet = signal('');
  readonly carrera = signal('');
  readonly estado = signal('');
  readonly carreras = computed(() => [...new Set(this.catalogo.estudiantes().map(e => e.carrera))].sort());
  readonly estados = computed(() => [...new Set(this.catalogo.estudiantes().map(e => e.estadoPera))].sort());
  readonly resultados = computed(() => {
    const texto = this.carnet().trim().toUpperCase();
    return this.catalogo.estudiantes().filter(e =>
      e.carnet.toUpperCase().includes(texto) &&
      (!this.carrera() || e.carrera === this.carrera()) &&
      (!this.estado() || e.estadoPera === this.estado()));
  });
  readonly detalle = signal<EstudianteListado | null>(null);
  readonly tieneRutaDetalle = this.router.config.some(r => r.path === 'expediente-detalle/:carnet');

  ngOnInit() {
    if (!this.permitido()) {
      const destino = this.acceso.perfil() ? '/inicio' : '/login';
      void this.router.navigateByUrl(destino, { replaceUrl: true });
    }
  }

  limpiar() {
    this.carnet.set('');
    this.carrera.set('');
    this.estado.set('');
  }

  verFicha(estudiante: EstudianteListado, dialogo: HTMLDialogElement) {
    this.detalle.set(estudiante);
    dialogo.showModal();
  }
}

