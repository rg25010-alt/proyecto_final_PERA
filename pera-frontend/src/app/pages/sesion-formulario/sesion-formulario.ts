import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

type Modalidad = 'Virtual' | 'Presencial';
type Estado = 'Programada' | 'Finalizada' | 'Cancelada';
interface Sesion {
  id: number; estudiante: string; carnet: string; materia: string;
  fecha: string; inicio: string; fin: string; modalidad: Modalidad;
  ubicacion: string; descripcion: string; estado: Estado;
}
type Formulario = Omit<Sesion, 'id' | 'estado'>;

@Component({
  selector: 'app-sesion-formulario',
  imports: [FormsModule, DatePipe],
  templateUrl: './sesion-formulario.html',
  styleUrl: './sesion-formulario.scss',
})
export class SesionFormulario {
  readonly hoy = new Date().toLocaleDateString('en-CA');
  readonly estudiantes = [
    { carnet: 'LM23001', nombre: 'María López Martínez' },
    { carnet: 'CR22018', nombre: 'Carlos Ramírez' },
    { carnet: 'AP24007', nombre: 'Ana Pérez' },
    { carnet: 'JG23042', nombre: 'José García' },
  ];
  readonly materias = ['Matemática I', 'Física I', 'Programación I', 'Base de Datos', 'Estadística'];
  readonly sesiones = signal<Sesion[]>([
    { id: 1, estudiante: 'María López Martínez', carnet: 'LM23001', materia: 'Matemática I',
      fecha: this.fechaDemo(2), inicio: '09:00', fin: '10:00', modalidad: 'Virtual',
      ubicacion: 'https://meet.google.com/ejemplo-pera', descripcion: 'Repaso de funciones', estado: 'Programada' },
    { id: 2, estudiante: 'Carlos Ramírez', carnet: 'CR22018', materia: 'Física I',
      fecha: this.fechaDemo(4), inicio: '14:00', fin: '15:00', modalidad: 'Presencial',
      ubicacion: 'Aula B-12', descripcion: 'Ejercicios de cinemática', estado: 'Programada' },
  ]);
  readonly editando = signal<number | null>(null);
  readonly aviso = signal('');
  readonly error = signal('');
  readonly filtro = signal('Todas');
  readonly sesionesFiltradas = computed(() =>
    this.sesiones().filter(s => this.filtro() === 'Todas' || s.estado === this.filtro())
      .sort((a, b) => (a.fecha + a.inicio).localeCompare(b.fecha + b.inicio))
  );

  formulario: Formulario = this.vacio();

  private fechaDemo(dias: number): string {
    const d = new Date();
    d.setDate(d.getDate() + dias);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
  private vacio(): Formulario {
    return { estudiante: '', carnet: '', materia: '', fecha: '', inicio: '', fin: '',
      modalidad: 'Presencial', ubicacion: '', descripcion: '' };
  }
  seleccionarEstudiante(carnet: string): void {
    const e = this.estudiantes.find(x => x.carnet === carnet);
    this.formulario.carnet = carnet;
    this.formulario.estudiante = e?.nombre ?? '';
  }
  cambiarModalidad(modalidad: Modalidad): void {
    this.formulario.modalidad = modalidad;
    this.formulario.ubicacion = '';
  }
  guardar(): void {
    this.error.set('');
    this.aviso.set('');
    const f = this.formulario;
    if (!f.carnet || !f.materia || !f.fecha || !f.inicio || !f.fin || !f.ubicacion.trim()) {
      this.error.set('Completa todos los campos obligatorios.'); return;
    }
    if (f.fecha < this.hoy) {
      this.error.set('La fecha no puede ser anterior a hoy.'); return;
    }
    if (f.fin <= f.inicio) {
      this.error.set('La hora de finalización debe ser posterior a la de inicio.'); return;
    }
    const conflicto = this.sesiones().some(s =>
      s.id !== this.editando() && s.estado === 'Programada' && s.fecha === f.fecha &&
      f.inicio < s.fin && f.fin > s.inicio &&
      (s.carnet === f.carnet)
    );
    if (conflicto) {
      this.error.set('El estudiante ya tiene una tutoría programada en ese horario.'); return;
    }
    const id = this.editando();
    if (id !== null) {
      this.sesiones.update(lista => lista.map(s => s.id === id ? { ...f, id, estado: s.estado } : s));
      this.aviso.set('Sesión actualizada en la demostración.');
    } else {
      const nuevoId = Math.max(0, ...this.sesiones().map(s => s.id)) + 1;
      this.sesiones.update(lista => [...lista, { ...f, id: nuevoId, estado: 'Programada' }]);
      this.aviso.set('Sesión registrada en la demostración.');
    }
    this.editando.set(null);
    this.formulario = this.vacio();
  }
  editar(s: Sesion): void {
    this.editando.set(s.id);
    this.formulario = { estudiante: s.estudiante, carnet: s.carnet, materia: s.materia,
      fecha: s.fecha, inicio: s.inicio, fin: s.fin, modalidad: s.modalidad,
      ubicacion: s.ubicacion, descripcion: s.descripcion };
    this.error.set(''); this.aviso.set('');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  cancelarEdicion(): void {
    this.editando.set(null);
    this.formulario = this.vacio();
    this.error.set(''); this.aviso.set('');
  }
  cancelarSesion(s: Sesion): void {
    if (!confirm('¿Cancelar esta sesión de tutoría?')) return;
    this.sesiones.update(lista => lista.map(x => x.id === s.id ? { ...x, estado: 'Cancelada' } : x));
    if (this.editando() === s.id) this.cancelarEdicion();
    this.aviso.set('Sesión cancelada en la demostración.');
  }
}
