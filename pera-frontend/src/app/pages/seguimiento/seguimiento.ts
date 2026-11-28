
import {
  Component,
  inject,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  ServicioSeguimiento,
  TutoriaSeguimiento
} from '../../services/seguimiento';

@Component({
  selector: 'app-seguimiento',
  imports: [CommonModule, FormsModule],
  templateUrl: './seguimiento.html',
  styleUrl: './seguimiento.scss'
})
export class Seguimiento implements OnInit {

  private readonly servicio = inject(ServicioSeguimiento);
  private readonly detector = inject(ChangeDetectorRef);

  tutorias: TutoriaSeguimiento[] = [];

  filtro: string = '';

  seleccionada: TutoriaSeguimiento | null = null;

  asistencia: string = 'Pendiente';

  nota: string = '';

  acuerdos: string = '';

  calificacion: number | null = null;

  mensaje: string = '';

  error: string = '';

  cargando: boolean = false;

  guardando: boolean = false;

  async ngOnInit() {

    await this.cargarTutorias();

  }

  // Consultar las tutorías registradas

  async cargarTutorias() {

    this.cargando = true;
    this.error = '';

    try {

      this.tutorias = await this.servicio.listar();

    } catch (error) {

      this.error = error instanceof Error
        ? error.message
        : 'No se pudieron cargar las tutorías.';

    } finally {

      this.cargando = false;
      this.detector.markForCheck();

    }

  }

  // Filtrar las tutorías

  get tutoriasFiltradas(): TutoriaSeguimiento[] {

    const texto = this.filtro.toLowerCase();

    return this.tutorias.filter(t =>
      t.estudiante.toLowerCase().includes(texto) ||
      t.materia.toLowerCase().includes(texto) ||
      t.carnet.toLowerCase().includes(texto)
    );

  }

  // Seleccionar una tutoría

  seleccionar(tutoria: TutoriaSeguimiento): void {

    if (this.guardando) {
      return;
    }

    this.seleccionada = tutoria;
    this.asistencia = tutoria.asistencia;
    this.nota = tutoria.nota;
    this.acuerdos = tutoria.acuerdos;
    this.calificacion = tutoria.calificacion;
    this.mensaje = '';
    this.error = '';

  }

  // Guardar asistencia y bitácora

  async guardar() {

    if (this.seleccionada === null || this.guardando) {
      return;
    }

    this.mensaje = '';
    this.error = '';

    if (this.asistencia !== 'Asistió' &&
        this.asistencia !== 'No asistió') {

      this.error = 'Selecciona el estado de asistencia.';
      return;

    }

    if (this.calificacion !== null &&
        (!Number.isFinite(this.calificacion) ||
         this.calificacion < 0 ||
         this.calificacion > 10)) {

      this.error = 'La calificación debe estar entre 0 y 10.';
      return;

    }

    const tutoria = this.seleccionada;

    const descripcion = this.nota.trim();
    const acuerdos = this.acuerdos.trim();
    const calificacion = this.calificacion;

    const cambioAsistencia =
      this.asistencia !== tutoria.asistencia;

    const cambioBitacora =
      descripcion !== tutoria.nota ||
      acuerdos !== tutoria.acuerdos ||
      calificacion !== tutoria.calificacion;

    if (cambioBitacora && !descripcion) {

      this.error = 'Escribe la descripción de la bitácora.';
      return;

    }

    if (!cambioAsistencia && !cambioBitacora) {

      this.mensaje = 'No hay cambios para guardar.';
      return;

    }

    this.guardando = true;

    let asistenciaGuardada = false;

    try {

      // Guardar la asistencia cuando fue modificada

      if (cambioAsistencia) {

        const resultado = await firstValueFrom(
          this.servicio.registrarAsistencia(
            tutoria.id,
            this.asistencia === 'Asistió',
            tutoria.observaciones
          )
        );

        tutoria.asistencia = resultado.asistio
          ? 'Asistió'
          : 'No asistió';

        this.asistencia = tutoria.asistencia;

        asistenciaGuardada = true;

      }

      // Registrar una bitácora si contiene cambios

      if (cambioBitacora) {

        const resultado = await firstValueFrom(
          this.servicio.registrarBitacora(
            tutoria.id,
            descripcion,
            calificacion,
            acuerdos
          )
        );

        tutoria.nota = resultado.descripcion;
        tutoria.acuerdos = acuerdos;
        tutoria.calificacion = resultado.calificacion;

        this.nota = tutoria.nota;
        this.acuerdos = tutoria.acuerdos;
        this.calificacion = tutoria.calificacion;

      }

      this.mensaje = 'Seguimiento guardado correctamente.';

    } catch (error) {

      const detalle = error instanceof Error
        ? error.message
        : 'No se pudo guardar el seguimiento.';

      if (asistenciaGuardada) {

        this.error = 'La asistencia se guardó, pero la bitácora no pudo confirmarse. ' + detalle;

      } else {

        this.error = detalle;

      }

    } finally {

      this.guardando = false;
      this.detector.markForCheck();

    }

  }

  // Cerrar el formulario

  limpiar(): void {

    if (this.guardando) {
      return;
    }

    this.seleccionada = null;
    this.asistencia = 'Pendiente';
    this.nota = '';
    this.acuerdos = '';
    this.calificacion = null;
    this.mensaje = '';
    this.error = '';

  }

}
