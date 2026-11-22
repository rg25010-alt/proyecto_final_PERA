import { Component, computed, inject, signal } from '@angular/core';
import { Acceso } from '../login/acceso';

type EstadoEgreso = 'Pendiente' | 'En revisión' | 'Habilitado' | 'Observado';
interface SolicitudEgreso {
  carnet: string;
  estudiante: string;
  carrera: string;
  cum: number;
  uvAprobadas: number;
  uvRequeridas: number;
  resultadoAcademico: string;
  estado: EstadoEgreso;
  observacion: string;
}

@Component({
  selector: 'app-habilitacion-egreso',
  imports: [],
  templateUrl: './habilitacion-egreso.html',
  styleUrl: './habilitacion-egreso.scss',
})
export class HabilitacionEgreso {
  private readonly acceso = inject(Acceso);
  readonly rol = computed(() => this.acceso.perfil()?.rol ?? '');
  // Solo para probar la pantalla sin iniciar sesión; nunca sustituye permisos reales.
  readonly rolDemo = signal<'Jefatura' | 'AdministracionAcademica'>('Jefatura');
  readonly rolActual = computed(() => this.rol() || this.rolDemo());
  readonly puedeRevisar = computed(() => this.rolActual() === 'Jefatura');
  readonly puedeRegistrar = computed(() => this.rolActual() === 'AdministracionAcademica');
  readonly permitido = computed(() => this.puedeRevisar() || this.puedeRegistrar());

  readonly buscar = signal('');
  readonly filtro = signal('Todos');
  readonly seleccion = signal<string | null>(null);
  readonly mensaje = signal('');
  readonly observacion = signal('');
  readonly estadoNuevo = signal<EstadoEgreso>('Pendiente');

  readonly solicitudes = signal<SolicitudEgreso[]>([
    { carnet: 'LM21015', estudiante: 'Laura Martínez', carrera: 'Ingeniería de Sistemas', cum: 8.4, uvAprobadas: 180, uvRequeridas: 180, resultadoAcademico: 'Cumple UV', estado: 'Pendiente', observacion: '' },
    { carnet: 'RP20032', estudiante: 'Ricardo Pérez', carrera: 'Ingeniería Industrial', cum: 7.1, uvAprobadas: 172, uvRequeridas: 180, resultadoAcademico: 'UV pendientes', estado: 'En revisión', observacion: 'Verificar asignaturas pendientes.' },
    { carnet: 'AC21007', estudiante: 'Andrea Cruz', carrera: 'Ingeniería de Sistemas', cum: 8.8, uvAprobadas: 180, uvRequeridas: 180, resultadoAcademico: 'Cumple UV', estado: 'Habilitado', observacion: 'Documentación verificada.' },
    { carnet: 'JS20019', estudiante: 'Jorge Sánchez', carrera: 'Ingeniería Industrial', cum: 6.9, uvAprobadas: 178, uvRequeridas: 180, resultadoAcademico: 'UV pendientes', estado: 'Observado', observacion: 'Faltan 2 UV.' },
  ]);

  readonly resultados = computed(() => {
    const texto = this.buscar().trim().toLowerCase();
    return this.solicitudes().filter(s =>
      (s.carnet.toLowerCase().includes(texto) || s.estudiante.toLowerCase().includes(texto)) &&
      (this.filtro() === 'Todos' || s.estado === this.filtro())
    );
  });
  readonly totalHabilitados = computed(() => this.solicitudes().filter(s => s.estado === 'Habilitado').length);
  readonly totalPendientes = computed(() => this.solicitudes().filter(s => s.estado === 'Pendiente' || s.estado === 'En revisión').length);
  readonly detalle = computed(() => this.solicitudes().find(s => s.carnet === this.seleccion()) ?? null);

  seleccionar(carnet: string) {
    const solicitud = this.solicitudes().find(s => s.carnet === carnet);
    if (!solicitud) return;
    this.seleccion.set(carnet);
    this.estadoNuevo.set(solicitud.estado);
    this.observacion.set(solicitud.observacion);
    this.mensaje.set('');
  }

  // Jefatura revisa el resultado; Administración Académica registra el estado.
  registrar() {
    if (!this.puedeRegistrar()) return;
    const actual = this.detalle();
    if (!actual) return;
    if (this.estadoNuevo() === 'Habilitado' && actual.uvAprobadas < actual.uvRequeridas) {
      this.mensaje.set('No se puede habilitar: el estudiante tiene UV pendientes.');
      return;
    }
    this.solicitudes.update(lista => lista.map(s =>
      s.carnet === actual.carnet
        ? { ...s, estado: this.estadoNuevo(), observacion: this.observacion().trim() }
        : s
    ));
    this.mensaje.set('Estado administrativo actualizado en los datos de demostración.');
  }
}
