import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Carrera, Expediente } from '../../models/expediente';
import { ServicioExpedientes } from '../../services/expedientes';
import { Acceso } from '../login/acceso';

@Component({
  selector: 'app-expediente-formulario',
  imports: [RouterLink],
  templateUrl: './expediente-formulario.html',
  styleUrl: './expediente-formulario.scss',
})
export class ExpedienteFormulario implements OnInit {
  private readonly acceso = inject(Acceso);
  private readonly api = inject(ServicioExpedientes);
  private readonly router = inject(Router);
  readonly permitido = computed(() => this.acceso.perfil()?.rol === 'AdministracionAcademica');
  readonly carreras = signal<Carrera[]>([]);
  readonly avisoCatalogo = signal('');
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly registrado = signal<Expediente | null>(null);

  ngOnInit() {
    if (this.permitido()) this.cargarCarreras();
    else void this.router.navigateByUrl(this.acceso.perfil() ? '/inicio' : '/login', { replaceUrl: true });
  }

  cargarCarreras() {
    this.avisoCatalogo.set('');
    this.api.listarCarreras().subscribe({
      next: carreras => {
        this.carreras.set(carreras);
        if (!carreras.length) this.avisoCatalogo.set('Todavía no hay carreras disponibles.');
      },
      error: () => this.avisoCatalogo.set('No se pudieron cargar las carreras.'),
    });
  }

  registrar(evento: Event, formulario: HTMLFormElement) {
    evento.preventDefault();
    if (!this.permitido() || this.guardando() || !formulario.reportValidity()) return;
    const campos = new FormData(formulario);
    this.error.set('');
    this.guardando.set(true);
    this.api.registrar({
      carnet: String(campos.get('carnet')).trim().toUpperCase(),
      nombre: String(campos.get('nombres')).trim(),
      apellidos: String(campos.get('apellidos')).trim(),
      correo: String(campos.get('correo')).trim(),
      clave: String(campos.get('clave')),
      carreraId: Number(campos.get('carreraId')),
      cum: Number(campos.get('cum')),
    }).pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: expediente => this.registrado.set(expediente),
      error: (respuesta: unknown) => {
        this.error.set(respuesta instanceof Error
          ? respuesta.message
          : 'No se pudo registrar el expediente. Volvé a intentarlo.');
      },
    });
  }
}
