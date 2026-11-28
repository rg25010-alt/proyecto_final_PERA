
import {
  Component,
  computed,
  signal,
  inject,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Acceso } from '../login/acceso';

import {
  ServicioNotificaciones,
  Aviso
} from '../../services/notificaciones';

@Component({
  selector: 'app-notificaciones',
  imports: [CommonModule, FormsModule],
  templateUrl: './notificaciones.html',
  styleUrl: './notificaciones.scss'
})
export class Notificaciones implements OnInit {

  private readonly servicio = inject(ServicioNotificaciones);

  private readonly acceso = inject(Acceso);

  readonly rol = signal('');

  readonly filtro = signal('Todas');

  readonly notificaciones = signal<Aviso[]>([]);

  readonly cargando = signal(false);

  readonly error = signal('');

  ngOnInit(): void {

    const perfil = this.acceso.perfil();

    switch (perfil?.rol) {

      case 'AdministracionAcademica':
        this.rol.set('Administración Académica');
        break;

      case 'CoordinacionPERA':
        this.rol.set('Coordinación PERA');
        break;

      case 'Jefatura':
        this.rol.set('Jefatura');
        break;

      case 'DocenteTutor':
        this.rol.set('Docente Tutor');
        break;

      case 'Estudiante':
        this.rol.set('Estudiante');
        break;

      default:
        return;

    }

    this.cargar();

  }

  cargar(): void {

    this.cargando.set(true);
    this.error.set('');
    this.notificaciones.set([]);

    this.servicio.listar(this.rol()).subscribe({

      next: datos => {

        this.notificaciones.set(datos);
        this.cargando.set(false);

      },

      error: error => {

        this.error.set(
          error instanceof Error
            ? error.message
            : 'No se pudieron cargar las notificaciones.'
        );

        this.cargando.set(false);

      }

    });

  }

  readonly visibles = computed(() =>

    this.notificaciones().filter(n =>

      this.filtro() === 'Todas' ||

      (this.filtro() === 'No leídas' && !n.leida) ||

      (this.filtro() === 'Leídas' && n.leida)

    )

  );

  readonly pendientes = computed(() =>

    this.notificaciones().filter(n => !n.leida).length

  );

  marcarLeida(id: number): void {

    this.servicio.marcarLeidas([id]);

    this.notificaciones.update(lista =>
      lista.map(n =>
        n.id === id ? { ...n, leida: true } : n
      )
    );

  }

  marcarTodas(): void {

    const ids = this.notificaciones().map(n => n.id);

    this.servicio.marcarLeidas(ids);

    this.notificaciones.update(lista =>
      lista.map(n => ({
        ...n,
        leida: true
      }))
    );

  }

}
