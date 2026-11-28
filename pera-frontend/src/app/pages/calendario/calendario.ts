
import { Component, computed, signal, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ServicioTutorias } from '../../services/tutorias';

type Modalidad = 'Virtual' | 'Presencial';

type Estado = 'Borrador' | 'Programada' | 'Finalizada' | 'Cancelada';

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
export class Calendario implements OnInit {

  private readonly servicio = inject(ServicioTutorias);

  private readonly hoy = new Date();

  readonly mesVisible = signal(
    new Date(this.hoy.getFullYear(), this.hoy.getMonth(), 1)
  );

  readonly modalidad = signal('Todas');
  readonly estado = signal('Todos');
  readonly seleccionada = signal<Sesion | null>(null);

  readonly diasSemana = [
    'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'
  ];

  readonly sesiones = signal<Sesion[]>([]);

  // Consultar las sesiones registradas en el backend

  ngOnInit(): void {

    this.servicio.listarSesiones().subscribe({

      next: datos => {

        this.sesiones.set(datos.map(s => {

          const fecha = new Date(s.fechaInicio);

          let estado = s.estadoSesion;

          if (estado === 'Realizada') {
            estado = 'Finalizada';
          }

          if (estado === 'Reprogramada') {
            estado = 'Programada';
          }

          return {
            id: Number(s.idSesion),
            fecha: this.fechaLocal(fecha),
            hora: fecha.toTimeString().slice(0, 5),
            materia: 'Tutoría #' + s.idSesion,
            tutor: 'No disponible',
            modalidad: s.modalidad as Modalidad,
            estado: estado as Estado,
            lugar: 'No disponible'
          };

        }));

      },

      error: error => {
        console.error('Error al consultar sesiones:', error);
      }

    });

  }

  readonly sesionesFiltradas = computed(() =>

    this.sesiones().filter(s =>

      (this.modalidad() === 'Todas' ||
        s.modalidad === this.modalidad()) &&

      (this.estado() === 'Todos' ||
        s.estado === this.estado())

    )

  );

  readonly celdas = computed(() => {

    const mes = this.mesVisible();

    const inicio = (mes.getDay() + 6) % 7;

    const cantidad = new Date(
      mes.getFullYear(),
      mes.getMonth() + 1,
      0
    ).getDate();

    return [
      ...Array.from({ length: inicio }, () => null),
      ...Array.from({ length: cantidad }, (_, i) => i + 1)
    ];

  });

  readonly proximas = computed(() =>

    this.sesionesFiltradas()

      .filter(s =>
        s.estado === 'Programada' &&
        s.fecha >= this.fechaLocal(this.hoy)
      )

      .sort((a, b) =>
        (a.fecha + a.hora).localeCompare(b.fecha + b.hora)
      )

      .slice(0, 4)

  );

  readonly nombreMes = computed(() =>

    new Intl.DateTimeFormat('es-SV', {
      month: 'long',
      year: 'numeric'
    }).format(this.mesVisible())

  );

  cambiarMes(cantidad: number): void {

    const mes = this.mesVisible();

    this.mesVisible.set(
      new Date(
        mes.getFullYear(),
        mes.getMonth() + cantidad,
        1
      )
    );

    this.seleccionada.set(null);

  }

  irHoy(): void {

    this.mesVisible.set(
      new Date(
        this.hoy.getFullYear(),
        this.hoy.getMonth(),
        1
      )
    );

  }

  fechaDia(dia: number): string {

    const mes = this.mesVisible();

    return this.fechaLocal(
      new Date(
        mes.getFullYear(),
        mes.getMonth(),
        dia
      )
    );

  }

  sesionesDia(dia: number): Sesion[] {

    const fecha = this.fechaDia(dia);

    return this.sesionesFiltradas().filter(
      s => s.fecha === fecha
    );

  }

  esHoy(dia: number): boolean {

    return this.fechaDia(dia) === this.fechaLocal(this.hoy);

  }

  private fechaLocal(fecha: Date): string {

    return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}-${String(fecha.getDate()).padStart(2, '0')}`;

  }

}
