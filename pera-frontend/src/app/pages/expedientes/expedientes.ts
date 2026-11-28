import { Component, OnInit, inject, signal } from '@angular/core';
import { Carrera, Expediente } from '../../models/expediente';
import { ServicioExpedientes } from '../../services/expedientes';
import { TutoresExpediente } from '../../components/tutores-expediente/tutores-expediente';

@Component({
  selector: 'app-expedientes',
  imports: [TutoresExpediente],
  templateUrl: './expedientes.html',
})
export class PaginaExpedientes implements OnInit {
  private readonly api = inject(ServicioExpedientes);
  readonly carreras = signal<Carrera[]>([]);
  readonly avisoCatalogo = signal('');
  readonly guardando = signal(false);
  readonly buscando = signal(false);
  readonly errorAlta = signal('');
  readonly confirmacion = signal('');
  readonly errorBusqueda = signal('');
  readonly resultado = signal<Expediente | null>(null);

  ngOnInit() {
    this.cargarCarreras();
  }

  cargarCarreras() {
    this.avisoCatalogo.set('');
    this.api.listarCarreras().subscribe({
      next: carreras => {
        this.carreras.set(carreras);
        if (!carreras.length) this.avisoCatalogo.set('El catálogo de carreras está vacío.');
      },
      error: () => this.avisoCatalogo.set('No se pudieron cargar las carreras.'),
    });
  }

  registrar(evento: Event, formulario: HTMLFormElement) {
    evento.preventDefault();
    if (this.guardando() || !formulario.reportValidity()) return;
    const campos = new FormData(formulario);
    this.errorAlta.set('');
    this.confirmacion.set('');
    this.guardando.set(true);
    this.api.registrar({
      carnet: String(campos.get('carnet')).trim().toUpperCase(),
      nombre: String(campos.get('nombres')).trim(),
      apellidos: String(campos.get('apellidos')).trim(),
      correo: String(campos.get('correo')).trim(),
      clave: String(campos.get('clave')),
      carreraId: Number(campos.get('carreraId')),
      cum: Number(campos.get('cum')),
    }).subscribe({
      next: expediente => {
        this.guardando.set(false);
        formulario.reset();
        this.confirmacion.set('Expediente registrado con el carné ' + expediente.carnet + '.');
      },
      error: (error: unknown) => {
        this.guardando.set(false);
        this.errorAlta.set(error instanceof Error
          ? error.message
          : 'No se pudo registrar el expediente.');
      },
    });
  }

  buscar(evento: Event, carnet: string) {
    evento.preventDefault();
    if (this.buscando() || !carnet.trim()) return;
    this.resultado.set(null);
    this.errorBusqueda.set('');
    this.buscando.set(true);
    this.api.buscar(carnet).subscribe({
      next: expediente => {
        this.buscando.set(false);
        if (expediente) this.resultado.set(expediente);
        else this.errorBusqueda.set('No se encontró un expediente con ese carné.');
      },
      error: (error: unknown) => {
        this.buscando.set(false);
        this.errorBusqueda.set(error instanceof Error
          ? error.message
          : 'No se pudo consultar el expediente.');
      },
    });
  }

  nombreCarrera(id: number) {
    return this.carreras().find(carrera => carrera.id === id)?.nombre ?? 'Carrera #' + id;
  }
}
