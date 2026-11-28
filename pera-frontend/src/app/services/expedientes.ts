
import { Injectable, inject, signal, effect } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';

import {
  map,
  Observable,
  of,
  filter,
  startWith,
  switchMap,
  catchError,
  firstValueFrom
} from 'rxjs';

import { Carrera, Expediente, NuevoExpediente } from '../models/expediente';

import { ServicioGraphQL } from './graphql';
import { ServicioTutorias } from './tutorias';
import { ServicioSeguimiento } from './seguimiento';

import { Acceso } from '../pages/login/acceso';

import {
  ListadoAutorizado,
  EstudianteListado
} from '../pages/estudiantes/listado-autorizado';

import {
  ConsultaDetalle,
  DetalleExpediente,
  SesionDelHistorial,
  NotaParcial
} from '../pages/expediente-detalle/consulta-detalle';

interface DatosCarreras {
  carreras: {
    idCarrera: number;
    nombreCarrera: string;
  }[];
}

interface DatosEstudiantes {
  estudiantes: {
    carnet: string;
    idCarrera: number;
    nombres: string;
    apellidos: string;
    cumActual: number;
  }[];
}

interface DatosRegistro {
  registrarEstudiante: {
    carnet: string;
    idCarrera: number;
    nombres: string;
    apellidos: string;
    correo: string;
    cumActual: number;
  };
}

@Injectable({ providedIn: 'root' })
export class ServicioExpedientes {

  private readonly graph = inject(ServicioGraphQL);

  listarCarreras() {

    return this.graph.post<DatosCarreras>(
      'query { carreras { idCarrera nombreCarrera } }',
    ).pipe(map(datos => datos.carreras.map((c): Carrera => ({
      id: c.idCarrera,
      nombre: c.nombreCarrera,
    }))));

  }

  buscar(carnet: string) {

    const filtro = carnet.trim().toUpperCase();

    return this.graph.post<DatosEstudiantes>(
      'query { estudiantes { carnet idCarrera nombres apellidos cumActual } }',
    ).pipe(map(datos => {

      const estudiante = datos.estudiantes.find(
        e => e.carnet === filtro
      );

      if (!estudiante) return null;

      return {
        carnet: estudiante.carnet,
        nombre: estudiante.nombres + ' ' + estudiante.apellidos,
        carreraId: estudiante.idCarrera,
        cum: estudiante.cumActual,
        estadoPera: '',
        topeUv: null,
      } satisfies Expediente;

    }));

  }

  registrar(datos: NuevoExpediente) {

    const mutation = `
      mutation RegistrarEstudiante($input: RegistrarEstudianteInput!) {
        registrarEstudiante(input: $input) {
          carnet idCarrera nombres apellidos correo cumActual
        }
      }
    `;

    const nombre = datos.nombre.trim();

    const apellidos = datos.apellidos?.trim()
      || nombre.split(' ').slice(1).join(' ').trim();

    const nombres = apellidos && nombre.endsWith(apellidos)
      ? nombre.slice(0, nombre.length - apellidos.length).trim()
      : nombre;

    return this.graph.post<DatosRegistro>(mutation, {

      input: {
        carnet: datos.carnet.trim().toUpperCase(),
        idCarrera: datos.carreraId,
        nombres,
        apellidos,
        correo: datos.correo?.trim() ?? '',
        clave: datos.clave ?? '',
        correoContacto: datos.correoContacto?.trim() || null,
        cumActual: datos.cum,
      },

    }).pipe(map(respuesta => {

      const estudiante = respuesta.registrarEstudiante;

      return {
        carnet: estudiante.carnet,
        nombre: estudiante.nombres + ' ' + estudiante.apellidos,
        carreraId: estudiante.idCarrera,
        cum: estudiante.cumActual,
        estadoPera: '',
        topeUv: null,
      } satisfies Expediente;

    }));

  }

}

// Datos necesarios para el listado de estudiantes

interface DatosListado {
  estudiantes: {
    carnet: string;
    nombres: string;
    apellidos: string;
    carrera: string;
    idDepartamento: number;
    cumActual: number;
  }[];

  departamentos: {
    idDepartamento: number;
    nombre: string;
  }[];

  asignacionesTutoria: {
    carnetEstudiante: string;
    estadoPera: string;
  }[];
}

// Consulta compartida del listado

function consultarListado(graph: ServicioGraphQL) {

  const query = `
    query {
      estudiantes {
        carnet
        nombres
        apellidos
        carrera
        idDepartamento
        cumActual
      }

      departamentos {
        idDepartamento
        nombre
      }

      asignacionesTutoria {
        carnetEstudiante
        estadoPera
      }
    }
  `;

  return graph.post<DatosListado>(query).pipe(

    map(datos => {

      const listado: EstudianteListado[] = [];

      for (const estudiante of datos.estudiantes) {

        const departamento = datos.departamentos.find(
          d => d.idDepartamento === estudiante.idDepartamento
        );

        const asignacion = datos.asignacionesTutoria.find(
          a => a.carnetEstudiante === estudiante.carnet
        );

        listado.push({

          carnet: estudiante.carnet,

          nombre: estudiante.nombres + ' ' + estudiante.apellidos,

          carrera: estudiante.carrera,

          departamento: departamento?.nombre || 'No disponible',

          cum: estudiante.cumActual,

          estadoPera: asignacion?.estadoPera || 'Sin asignación'

        });

      }

      return listado;

    })

  );

}

// Implementacion del listado autorizado

@Injectable()
export class ListadoAutorizadoGraphQL extends ListadoAutorizado {

  private readonly graph = inject(ServicioGraphQL);
  private readonly acceso = inject(Acceso);
  private readonly router = inject(Router);

  readonly estudiantes = signal<EstudianteListado[]>([]);

  constructor() {

    super();

    effect(onCleanup => {

      const perfil = this.acceso.perfil();

      this.estudiantes.set([]);

      if (!perfil || perfil.rol === 'Estudiante') {
        return;
      }

      const suscripcion = this.router.events.pipe(

        filter(evento => evento instanceof NavigationEnd),

        map(evento => evento.urlAfterRedirects),

        startWith(this.router.url),

        switchMap(url => {

          const ruta = url.split('?')[0];

          if (
            ruta === '/estudiantes' ||
            ruta === '/asignacion-tutores'
          ) {

            return consultarListado(this.graph).pipe(

              catchError(error => {

                console.error(
                  'Error al consultar estudiantes:',
                  error
                );

                return of([]);

              })

            );

          }

          return of([]);

        })

      ).subscribe(datos => {

        this.estudiantes.set(datos);

      });

      onCleanup(() => suscripcion.unsubscribe());

    });

  }

}

// Datos adicionales del expediente

interface DatosDetalle {

  reglasUv: {
    cumMin: number;
    cumMax: number;
    uvMax: number;
    vigente: boolean;
  }[];

  tutoresAsignados: {
    idDocente: number;
    nombres: string;
    apellidos: string;
    areaRefuerzo: string;
  }[];

}

interface DatosAsistenciasExpediente {

  asistencias: {
    idParticipacion: number;
    carnetEstudiante: string;
    asistio: boolean | null;
  }[];

}

// Implementacion del detalle del expediente

@Injectable()
export class ConsultaDetalleGraphQL extends ConsultaDetalle {

  private readonly graph = inject(ServicioGraphQL);

  private readonly tutorias = inject(ServicioTutorias);

  private readonly seguimiento = inject(ServicioSeguimiento);

  porCarnet(carnet: string): Observable<DetalleExpediente | null> {

    carnet = carnet.trim().toUpperCase();

    return consultarListado(this.graph).pipe(

      switchMap(estudiantes => {

        const estudiante = estudiantes.find(
          e => e.carnet === carnet
        );

        if (!estudiante) {
          return of(null);
        }

        const query = `
          query Detalle($carnet: String!) {

            reglasUv {
              cumMin
              cumMax
              uvMax
              vigente
            }

            tutoresAsignados(carnet: $carnet) {
              idDocente
              nombres
              apellidos
              areaRefuerzo
            }

          }
        `;

        return this.graph.post<DatosDetalle>(
          query,
          { carnet }
        ).pipe(

          switchMap(async datos => {

            const regla = datos.reglasUv.find(r =>
              r.vigente &&
              estudiante.cum >= r.cumMin &&
              estudiante.cum <= r.cumMax
            );

            const registros = await this.consultarHistorial(carnet);

            const expediente: DetalleExpediente = {

              ...estudiante,

              topeUv: regla?.uvMax ?? null,

              tutoresAsignados: datos.tutoresAsignados.map(t => ({

                tutor: {
                  id: Number(t.idDocente),
                  nombre: t.nombres + ' ' + t.apellidos
                },

                areaRefuerzo: t.areaRefuerzo

              })),

              historial: registros.historial,

              notas: registros.notas

            };

            return expediente;

          })

        );

      })

    );

  }

  // Consultar historial de tutorias y calificaciones

  private async consultarHistorial(carnet: string) {

    const historial: SesionDelHistorial[] = [];

    const notas: NotaParcial[] = [];

    const sesiones = await firstValueFrom(
      this.tutorias.listarSesiones()
    );

    for (const sesion of sesiones) {

      if (
        sesion.estadoSesion === 'Borrador' ||
        sesion.estadoSesion === 'Cancelada'
      ) {
        continue;
      }

      const datos = await firstValueFrom(

        this.graph.post<DatosAsistenciasExpediente>(`

          query Asistencias($idSesion: Long!) {

            asistencias(idSesion: $idSesion) {
              idParticipacion
              carnetEstudiante
              asistio
            }

          }

        `, {

          idSesion: Number(sesion.idSesion)

        })

      );

      const participantes = datos.asistencias.filter(
        a => a.carnetEstudiante === carnet
      );

      for (const participante of participantes) {

        const bitacoras = await firstValueFrom(

          this.seguimiento.consultarBitacoras(
            Number(participante.idParticipacion)
          )

        );

        let asistencia = 'Pendiente';

        if (participante.asistio === true) {
          asistencia = 'Asistió';
        }

        if (participante.asistio === false) {
          asistencia = 'No asistió';
        }

        const ultima = bitacoras[0];

        historial.push({

          id: Number(participante.idParticipacion),

          fecha: sesion.fechaInicio,

          tutor: 'No disponible',

          asistencia: asistencia,

          tema: ultima?.descripcion ||
            'Tutoría #' + sesion.idSesion,

          acuerdos: ultima?.acuerdos ?? ''

        });

        for (const bitacora of bitacoras) {

          if (
            bitacora.calificacion !== null &&
            bitacora.calificacion !== undefined
          ) {

            notas.push({

              id: Number(bitacora.idBitacora),

              fecha: bitacora.fechaRegistro,

              actividad: bitacora.descripcion,

              nota: Number(bitacora.calificacion)

            });

          }

        }

      }

    }

    historial.sort((a, b) =>
      b.fecha.localeCompare(a.fecha)
    );

    notas.sort((a, b) =>
      b.fecha.localeCompare(a.fecha)
    );

    return {
      historial,
      notas
    };

  }

}
