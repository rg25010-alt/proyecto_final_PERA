
import { Component, computed, signal, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ServicioTutorias, EstudianteSesion } from '../../services/tutorias';

type Modalidad = 'Virtual' | 'Presencial';

type Estado = 'Borrador' | 'Programada' | 'Finalizada' | 'Cancelada';

interface Sesion {
  id: number;
  estudiante: string;
  carnet: string;
  materia: string;
  fecha: string;
  inicio: string;
  fin: string;
  modalidad: Modalidad;
  ubicacion: string;
  descripcion: string;
  estado: Estado;
}

type Formulario = Omit<Sesion, 'id' | 'estado'>;

@Component({
  selector: 'app-sesion-formulario',
  imports: [FormsModule, DatePipe],
  templateUrl: './sesion-formulario.html',
  styleUrl: './sesion-formulario.scss',
})
export class SesionFormulario implements OnInit {

  private readonly servicio = inject(ServicioTutorias);

  private idDocente = 0;

  readonly hoy = this.fechaLocal(new Date());

  estudiantes: EstudianteSesion[] = [];

  materias: string[] = [];

  readonly sesiones = signal<Sesion[]>([]);

  readonly editando = signal<number | null>(null);
  readonly aviso = signal('');
  readonly error = signal('');
  readonly filtro = signal('Todas');
  readonly guardando = signal(false);

  readonly sesionesFiltradas = computed(() =>

    this.sesiones().filter(s =>
      this.filtro() === 'Todas' ||
      s.estado === this.filtro()
    ).sort((a, b) =>
      (a.fecha + a.inicio).localeCompare(b.fecha + b.inicio)
    )

  );

  formulario: Formulario = this.vacio();

  // Cargar los estudiantes y las sesiones

  async ngOnInit() {

    try {

      const datos = await this.servicio.estudiantesAsignados();

      this.idDocente = datos.idDocente;
      this.estudiantes = datos.estudiantes;

    } catch (error) {

      this.error.set(
        error instanceof Error
          ? error.message
          : 'No se pudieron cargar los estudiantes.'
      );

    }

    await this.cargarSesiones();

  }

  // Consultar las sesiones registradas

  async cargarSesiones() {

    try {

      const datos = await firstValueFrom(
        this.servicio.listarSesiones()
      );

      const sesiones: Sesion[] = datos.map(s => {

        const inicio = new Date(s.fechaInicio);
        const fin = new Date(s.fechaFin);

        return {

          id: Number(s.idSesion),

          estudiante: 'No disponible',

          carnet: '',

          materia: 'Tutoría #' + s.idSesion,

          fecha: this.fechaLocal(inicio),

          inicio: inicio.toTimeString().slice(0, 5),

          fin: fin.toTimeString().slice(0, 5),

          modalidad: s.modalidad as Modalidad,

          ubicacion: '',

          descripcion: '',

          estado: s.estadoSesion as Estado

        };

      });

      this.sesiones.set(sesiones);

    } catch {

      this.error.set('No se pudieron cargar las sesiones.');

    }

  }

  // Obtener la fecha en formato año-mes-día

  private fechaLocal(fecha: Date): string {

    const anio = fecha.getFullYear();

    const mes = String(
      fecha.getMonth() + 1
    ).padStart(2, '0');

    const dia = String(
      fecha.getDate()
    ).padStart(2, '0');

    return anio + '-' + mes + '-' + dia;

  }

  private vacio(): Formulario {

    return {
      estudiante: '',
      carnet: '',
      materia: '',
      fecha: '',
      inicio: '',
      fin: '',
      modalidad: 'Presencial',
      ubicacion: '',
      descripcion: ''
    };

  }

  seleccionarEstudiante(carnet: string): void {

    const e = this.estudiantes.find(x =>
      x.carnet === carnet
    );

    this.formulario.carnet = carnet;
    this.formulario.estudiante = e?.nombre ?? '';
    this.formulario.materia = '';

    this.materias = e
      ? [...new Set(e.asignaciones.map(a => a.areaRefuerzo))]
      : [];

  }

  cambiarModalidad(modalidad: Modalidad): void {

    this.formulario.modalidad = modalidad;
    this.formulario.ubicacion = '';

  }

  // Validar y registrar una sesión

  async guardar() {

    if (this.guardando()) {
      return;
    }

    this.error.set('');
    this.aviso.set('');

    const f = this.formulario;

    if (!f.carnet || !f.materia || !f.fecha ||
        !f.inicio || !f.fin || !f.ubicacion.trim()) {

      this.error.set('Completa todos los campos obligatorios.');
      return;

    }

    if (f.fecha < this.hoy) {

      this.error.set('La fecha no puede ser anterior a hoy.');
      return;

    }

    if (f.fin <= f.inicio) {

      this.error.set(
        'La hora de finalización debe ser posterior a la de inicio.'
      );

      return;

    }

    if (f.descripcion.trim()) {

      this.error.set(
        'El objetivo de la sesión todavía no se puede guardar. Deja ese campo vacío.'
      );

      return;

    }

    const estudiante = this.estudiantes.find(e =>
      e.carnet === f.carnet
    );

    const asignaciones = estudiante?.asignaciones.filter(a =>
      a.areaRefuerzo === f.materia
    ) || [];

    if (asignaciones.length !== 1 || !this.idDocente) {

      this.error.set(
        'No se pudo identificar la asignación del estudiante.'
      );

      return;

    }

    const asignacion = asignaciones[0];

    this.guardando.set(true);

    try {

      const sesion = await firstValueFrom(

        this.servicio.programarSesion({

          idAsignacion: asignacion.idAsignacion,

          fechaInicio: new Date(
            f.fecha + 'T' + f.inicio
          ).toISOString(),

          fechaFin: new Date(
            f.fecha + 'T' + f.fin
          ).toISOString(),

          modalidad: f.modalidad,

          ubicacionEnlace: f.ubicacion.trim(),

          idDocenteResponsable: this.idDocente,

          idDocentes: [this.idDocente],

          idAsignacionesParticipantes: [
            asignacion.idAsignacion
          ]

        })

      );

      this.formulario = this.vacio();
      this.materias = [];
      this.editando.set(null);

      try {

        await firstValueFrom(
          this.servicio.publicarSesion(
            Number(sesion.idSesion)
          )
        );

        this.aviso.set('Sesión programada correctamente.');

      } catch {

        this.error.set(
          'La sesión fue registrada como borrador, pero no pudo publicarse.'
        );

      }

      await this.cargarSesiones();

    } catch (error) {

      this.error.set(
        error instanceof Error
          ? error.message
          : 'No se pudo programar la sesión.'
      );

    } finally {

      this.guardando.set(false);

    }

  }

  editar(s: Sesion): void {

    this.error.set(
      'La edición de sesiones todavía no está disponible.'
    );

    this.aviso.set('');

  }

  cancelarEdicion(): void {

    this.editando.set(null);
    this.formulario = this.vacio();
    this.materias = [];
    this.error.set('');
    this.aviso.set('');

  }

  cancelarSesion(s: Sesion): void {

    this.error.set(
      'La cancelación de sesiones todavía no está disponible.'
    );

    this.aviso.set('');

  }

}
