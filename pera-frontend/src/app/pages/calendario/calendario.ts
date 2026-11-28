import { Component, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';

type Modalidad = 'Virtual' | 'Presencial';
type Estado = 'Programada' | 'Finalizada' | 'Cancelada';
interface Sesion {
  id: number;
  fecha: string;
  hora: string;
  materia: string;
  tutor: string;
  modalidad: Modalidad;
  estado: Estado;
  lugar: string;
}

@Component({
  selector: 'app-calendario',
  imports: [DatePipe],
  templateUrl: './calendario.html',
  styleUrl: './calendario.scss',
})
export class Calendario {
  private readonly hoy = new Date();
  readonly mesVisible = signal(new Date(this.hoy.getFullYear(), this.hoy.getMonth(), 1));
  readonly modalidad = signal('Todas');
  readonly estado = signal('Todos');
  readonly seleccionada = signal<Sesion | null>(null);
  readonly diasSemana = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];

  private fechaDemo(dia: number): string {
    const d = new Date(this.hoy.getFullYear(), this.hoy.getMonth(), dia);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  readonly sesiones: Sesion[] = [
    { id: 1, fecha: this.fechaDemo(3), hora: '08:00', materia: 'Matemática I', tutor: 'Lic. Ana Martínez', modalidad: 'Presencial', estado: 'Finalizada', lugar: 'Aula B-12' },
    { id: 2, fecha: this.fechaDemo(8), hora: '10:00', materia: 'Programación', tutor: 'Ing. Carlos López', modalidad: 'Virtual', estado: 'Programada', lugar: 'Enlace por confirmar' },
    { id: 3, fecha: this.fechaDemo(12), hora: '14:00', materia: 'Física I', tutor: 'Lic. María Hernández', modalidad: 'Presencial', estado: 'Programada', lugar: 'Laboratorio 2' },
    { id: 4, fecha: this.fechaDemo(17), hora: '09:00', materia: 'Estadística', tutor: 'Lic. José Ramírez', modalidad: 'Virtual', estado: 'Cancelada', lugar: 'Sin enlace' },
    { id: 5, fecha: this.fechaDemo(22), hora: '11:00', materia: 'Matemática I', tutor: 'Lic. Ana Martínez', modalidad: 'Virtual', estado: 'Programada', lugar: 'Enlace por confirmar' },
    { id: 6, fecha: this.fechaDemo(26), hora: '15:00', materia: 'Programación', tutor: 'Ing. Carlos López', modalidad: 'Presencial', estado: 'Programada', lugar: 'Aula C-04' },
  ];

  readonly sesionesFiltradas = computed(() => this.sesiones.filter(s =>
    (this.modalidad() === 'Todas' || s.modalidad === this.modalidad()) &&
    (this.estado() === 'Todos' || s.estado === this.estado())
  ));

  readonly celdas = computed(() => {
    const mes = this.mesVisible();
    const inicio = (mes.getDay() + 6) % 7;
    const cantidad = new Date(mes.getFullYear(), mes.getMonth() + 1, 0).getDate();
    return [...Array.from({ length: inicio }, () => null),
      ...Array.from({ length: cantidad }, (_, i) => i + 1)];
  });

  readonly proximas = computed(() => this.sesionesFiltradas()
    .filter(s => s.estado === 'Programada' && s.fecha >= this.fechaLocal(this.hoy))
    .sort((a, b) => (a.fecha + a.hora).localeCompare(b.fecha + b.hora))
    .slice(0, 4));

  readonly nombreMes = computed(() => new Intl.DateTimeFormat('es-SV', { month: 'long', year: 'numeric' }).format(this.mesVisible()));

  cambiarMes(cantidad: number): void {
    const mes = this.mesVisible();
    this.mesVisible.set(new Date(mes.getFullYear(), mes.getMonth() + cantidad, 1));
    this.seleccionada.set(null);
  }

  irHoy(): void {
    this.mesVisible.set(new Date(this.hoy.getFullYear(), this.hoy.getMonth(), 1));
  }

  fechaDia(dia: number): string {
    const mes = this.mesVisible();
    return this.fechaLocal(new Date(mes.getFullYear(), mes.getMonth(), dia));
  }

  sesionesDia(dia: number): Sesion[] {
    const fecha = this.fechaDia(dia);
    return this.sesionesFiltradas().filter(s => s.fecha === fecha);
  }

  esHoy(dia: number): boolean { return this.fechaDia(dia) === this.fechaLocal(this.hoy); }

  private fechaLocal(fecha: Date): string {
    return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}-${String(fecha.getDate()).padStart(2, '0')}`;
  }
}
