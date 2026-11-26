import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-mi-pera',
  imports: [CommonModule],
  templateUrl: './mi-pera.html',
  styleUrl: './mi-pera.scss'
})
export class MiPera {
  // Información de ejemplo. Luego se obtendrá de la base de datos.
  readonly estudiante = {
    nombre: 'María Fernanda López',
    carnet: 'LM23001',
    carrera: 'Ingeniería de Sistemas Informáticos',
    cum: 7.85,
    uvAprobadas: 96,
    uvInscritas: 20,
    estado: 'En seguimiento'
  };

  readonly tutores = [
    { nombre: 'Lic. Ana Martínez', asignatura: 'Matemática I', correo: 'ana.martinez@ejemplo.edu.sv' },
    { nombre: 'Ing. Roberto Pérez', asignatura: 'Programación I', correo: 'roberto.perez@ejemplo.edu.sv' }
  ];

  readonly historial = [
    { fecha: '2026-11-04', actividad: 'Tutoría de Matemática I', detalle: 'Repaso de funciones', estado: 'Completada' },
    { fecha: '2026-11-10', actividad: 'Tutoría de Programación I', detalle: 'Ejercicios de ciclos', estado: 'Completada' },
    { fecha: '2026-11-26', actividad: 'Tutoría de Matemática I', detalle: 'Preparación de evaluación', estado: 'Programada' }
  ];

  readonly mostrarHistorial = signal(true);

  cambiarHistorial() {
    this.mostrarHistorial.update(valor => !valor);
  }
}
