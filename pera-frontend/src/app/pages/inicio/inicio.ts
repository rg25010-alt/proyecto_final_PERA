import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Acceso, Rol } from '../login/acceso';

interface Modulo { titulo: string; ruta: string }
interface InicioRol { titulo: string; resumen: string; modulos: Modulo[] }

const estudiantes = { titulo: 'Listado de estudiantes', ruta: 'estudiantes' };
const calendario = { titulo: 'Calendario', ruta: 'calendario' };
const notificaciones = { titulo: 'Notificaciones', ruta: 'notificaciones' };
const egreso = { titulo: 'Habilitación de egreso', ruta: 'habilitacion-egreso' };

const inicioPorRol: Record<Rol, InicioRol> = {
  AdministracionAcademica: {
    titulo: 'Administración Académica',
    resumen: 'Registrá expedientes, consultá los datos académicos y revisá los trámites de egreso.',
    modulos: [{ titulo: 'Registrar expediente', ruta: 'expediente-formulario' }, estudiantes, egreso, notificaciones],
  },
  CoordinacionPERA: {
    titulo: 'Coordinación PERA',
    resumen: 'Consultá los expedientes, asigná tutores y revisá las alertas de seguimiento.',
    modulos: [estudiantes, { titulo: 'Asignar tutores', ruta: 'asignacion-tutores' }, notificaciones],
  },
  Jefatura: {
    titulo: 'Jefatura',
    resumen: 'Revisá los estudiantes, docentes y resultados de tu departamento.',
    modulos: [estudiantes, { titulo: 'Administrar docentes', ruta: 'docentes' },
      { titulo: 'Reportes', ruta: 'reportes' }, egreso, notificaciones],
  },
  DocenteTutor: {
    titulo: 'Docente Tutor',
    resumen: 'Consultá los estudiantes autorizados y organizá las sesiones de tutoría.',
    modulos: [estudiantes, calendario, { titulo: 'Programar sesión', ruta: 'sesion-formulario' },
      { titulo: 'Seguimiento de tutorías', ruta: 'seguimiento' }, notificaciones],
  },
  Estudiante: {
    titulo: 'Estudiante',
    resumen: 'Consultá tu estado académico, tus tutores y las sesiones programadas.',
    modulos: [{ titulo: 'Mi PERA', ruta: 'mi-pera' }, calendario, notificaciones],
  },
};

@Component({
  selector: 'app-inicio',
  imports: [RouterLink],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss',
})
export class Inicio implements OnInit {
  readonly acceso = inject(Acceso);
  private readonly router = inject(Router);
  readonly vistas = inicioPorRol;
  readonly error = signal('');
  readonly saliendo = signal(false);

  ngOnInit() {
    if (!this.acceso.perfil()) void this.router.navigateByUrl('/login', { replaceUrl: true });
  }

  disponible(ruta: string) {
    return this.router.config.some(entrada => entrada.path === ruta);
  }

  async salir() {
    if (this.saliendo()) return;
    this.saliendo.set(true);
    this.error.set('');
    try {
      await this.acceso.salir();
      await this.router.navigateByUrl('/login', { replaceUrl: true });
    } catch {
      this.error.set('No se pudo cerrar la sesión. Volvé a intentarlo.');
    } finally {
      this.saliendo.set(false);
    }
  }
}
