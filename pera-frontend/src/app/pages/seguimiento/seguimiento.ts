import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Tutoria {
  id: number;
  estudiante: string;
  materia: string;
  fecha: string;
  asistencia: string;
  nota: string;
  acuerdos: string;
  calificacion: string;
}

@Component({
  selector: 'app-seguimiento',
  imports: [CommonModule, FormsModule],
  templateUrl: './seguimiento.html',
  styleUrl: './seguimiento.scss'
})
export class Seguimiento {
  // Datos ficticios: después vendrán de la base de datos.
  tutorias: Tutoria[] = [
    { id: 1, estudiante: 'María López', materia: 'Matemática I', fecha: '2026-11-18', asistencia: 'Asistió', nota: 'Se repasaron ecuaciones.', acuerdos: 'Resolver guía de ejercicios.', calificacion: '8' },
    { id: 2, estudiante: 'Carlos Ramírez', materia: 'Física I', fecha: '2026-11-19', asistencia: 'Pendiente', nota: '', acuerdos: '', calificacion: '' },
    { id: 3, estudiante: 'Ana Pérez', materia: 'Programación I', fecha: '2026-11-20', asistencia: 'No asistió', nota: 'Estudiante ausente.', acuerdos: 'Reprogramar tutoría.', calificacion: '' }
  ];

  filtro: string = '';
  seleccionada: Tutoria | null = null;
  asistencia: string = 'Pendiente';
  nota: string = '';
  acuerdos: string = '';
  calificacion: string = '';
  mensaje: string = '';

  // Muestra las tutorías que coinciden con la búsqueda.
  get tutoriasFiltradas(): Tutoria[] {
    return this.tutorias.filter(t =>
      t.estudiante.toLowerCase().includes(this.filtro.toLowerCase()) ||
      t.materia.toLowerCase().includes(this.filtro.toLowerCase())
    );
  }

  seleccionar(tutoria: Tutoria): void {
    this.seleccionada = tutoria;
    this.asistencia = tutoria.asistencia;
    this.nota = tutoria.nota;
    this.acuerdos = tutoria.acuerdos;
    this.calificacion = tutoria.calificacion;
    this.mensaje = '';
  }

  guardar(): void {
    if (this.seleccionada === null) {
      return;
    }
    if (this.calificacion !== '' && (Number(this.calificacion) < 0 || Number(this.calificacion) > 10)) {
      this.mensaje = 'La calificación debe estar entre 0 y 10.';
      return;
    }
    this.seleccionada.asistencia = this.asistencia;
    this.seleccionada.nota = this.nota;
    this.seleccionada.acuerdos = this.acuerdos;
    this.seleccionada.calificacion = this.calificacion;
    this.mensaje = 'Seguimiento guardado (solo datos de prueba).';
  }

  limpiar(): void {
    this.seleccionada = null;
    this.mensaje = '';
  }
}
