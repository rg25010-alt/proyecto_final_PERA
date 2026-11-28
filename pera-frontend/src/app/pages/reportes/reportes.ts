import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reportes',
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.html',
  styleUrl: './reportes.scss'
})
export class Reportes {
  // Datos ficticios para mostrar cómo funcionarían los reportes.
  readonly ciclo = signal('Todos');
  readonly carrera = signal('Todas');

  readonly estudiantes = [
    { nombre: 'María López', carnet: 'LM23001', carrera: 'Sistemas', ciclo: '2026-II', nota: 8.5, asistencia: 90 },
    { nombre: 'Carlos Ramírez', carnet: 'CR22018', carrera: 'Sistemas', ciclo: '2026-II', nota: 5.4, asistencia: 60 },
    { nombre: 'Ana Pérez', carnet: 'AP24007', carrera: 'Industrial', ciclo: '2026-II', nota: 7.8, asistencia: 100 },
    { nombre: 'José García', carnet: 'JG23042', carrera: 'Industrial', ciclo: '2026-II', nota: 4.5, asistencia: 50 },
    { nombre: 'Lucía Hernández', carnet: 'LH22012', carrera: 'Sistemas', ciclo: '2026-I', nota: 9.2, asistencia: 95 },
    { nombre: 'Pedro Morales', carnet: 'PM24015', carrera: 'Industrial', ciclo: '2026-I', nota: 6.8, asistencia: 80 },
    { nombre: 'Sofía Torres', carnet: 'ST23009', carrera: 'Sistemas', ciclo: '2026-I', nota: 7.1, asistencia: 85 },
    { nombre: 'Daniel Ruiz', carnet: 'DR22025', carrera: 'Industrial', ciclo: '2026-I', nota: 5.7, asistencia: 65 }
  ];

  readonly datosFiltrados = computed(() => this.estudiantes.filter(estudiante =>
    (this.ciclo() === 'Todos' || estudiante.ciclo === this.ciclo()) &&
    (this.carrera() === 'Todas' || estudiante.carrera === this.carrera())
  ));

  readonly aprobados = computed(() => this.datosFiltrados().filter(e => e.nota >= 6).length);
  readonly reprobados = computed(() => this.datosFiltrados().filter(e => e.nota < 6).length);
  readonly promedioAsistencia = computed(() => {
    const datos = this.datosFiltrados();
    if (!datos.length) return 0;
    return Math.round(datos.reduce((total, e) => total + e.asistencia, 0) / datos.length);
  });
  readonly porcentajeAprobados = computed(() => {
    const total = this.datosFiltrados().length;
    return total ? Math.round(this.aprobados() * 100 / total) : 0;
  });

  limpiarFiltros() {
    this.ciclo.set('Todos');
    this.carrera.set('Todas');
  }
}
