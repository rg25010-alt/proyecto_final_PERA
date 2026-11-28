import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-notificaciones',
  imports: [CommonModule, FormsModule],
  templateUrl: './notificaciones.html',
  styleUrl: './notificaciones.scss'
})
export class Notificaciones {
  // Cambia este rol para probar lo que ve cada usuario.
  readonly rol = signal('Estudiante');
  readonly filtro = signal('Todas');

  // Datos ficticios. No hay conexión con el servidor.
  readonly notificaciones = signal([
    { id: 1, titulo: 'Tutoría programada', mensaje: 'Tienes una tutoría de Matemática I el 25/11/2026 a las 9:00 a. m.',
      fecha: '21/11/2026', tipo: 'Sesión', destinatario: 'Estudiante', leida: false },
    { id: 2, titulo: 'Cambio de horario', mensaje: 'La tutoría de Programación I cambió a las 2:00 p. m.',
      fecha: '20/11/2026', tipo: 'Sesión', destinatario: 'Estudiante', leida: false },
    { id: 3, titulo: 'Tutoría registrada', mensaje: 'Se registró una nueva sesión con un estudiante asignado.',
      fecha: '19/11/2026', tipo: 'Sesión', destinatario: 'Docente Tutor', leida: false },
    { id: 4, titulo: 'Alerta temprana', mensaje: 'Un estudiante presenta un CUM que requiere revisión y seguimiento.',
      fecha: '18/11/2026', tipo: 'Alerta temprana', destinatario: 'Coordinación PERA', leida: false },
    { id: 5, titulo: 'Seguimiento actualizado', mensaje: 'Se ha actualizado el seguimiento de tu tutoría.',
      fecha: '17/11/2026', tipo: 'Seguimiento', destinatario: 'Estudiante', leida: true },
    { id: 6, titulo: 'Alerta revisada', mensaje: 'La alerta de seguimiento académico fue revisada.',
      fecha: '16/11/2026', tipo: 'Alerta temprana', destinatario: 'Coordinación PERA', leida: true }
  ]);

  readonly visibles = computed(() => this.notificaciones().filter(n => {
    const esDeRol = n.destinatario === this.rol();
    const esDelFiltro = this.filtro() === 'Todas' ||
      (this.filtro() === 'No leídas' && !n.leida) ||
      (this.filtro() === 'Leídas' && n.leida);
    return esDeRol && esDelFiltro;
  }));

  readonly pendientes = computed(() =>
    this.notificaciones().filter(n => n.destinatario === this.rol() && !n.leida).length
  );

  marcarLeida(id: number) {
    this.notificaciones.update(lista => lista.map(n =>
      n.id === id ? { ...n, leida: true } : n
    ));
  }

  marcarTodas() {
    this.notificaciones.update(lista => lista.map(n =>
      n.destinatario === this.rol() ? { ...n, leida: true } : n
    ));
  }
}
