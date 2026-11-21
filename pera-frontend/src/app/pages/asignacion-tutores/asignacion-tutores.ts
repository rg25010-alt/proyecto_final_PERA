import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TutoresExpediente } from '../../components/tutores-expediente/tutores-expediente';
import { ListadoAutorizado } from '../estudiantes/listado-autorizado';
import { Acceso } from '../login/acceso';

@Component({
  selector: 'app-asignacion-tutores',
  imports: [RouterLink, TutoresExpediente],
  templateUrl: './asignacion-tutores.html',
  styleUrl: './asignacion-tutores.scss',
})
export class AsignacionTutores implements OnInit {
  private readonly acceso = inject(Acceso);
  private readonly router = inject(Router);
  readonly listado = inject(ListadoAutorizado);
  readonly carnet = signal('');
  readonly permitido = computed(() => this.acceso.perfil()?.rol === 'CoordinacionPERA');
  readonly seleccion = computed(() => this.listado.estudiantes().filter(e => e.carnet === this.carnet()));

  ngOnInit() {
    if (!this.permitido()) {
      void this.router.navigateByUrl(this.acceso.perfil() ? '/inicio' : '/login', { replaceUrl: true });
    }
  }
}

