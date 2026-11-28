
import { Injectable, inject } from '@angular/core';
import { forkJoin, map, switchMap, firstValueFrom } from 'rxjs';
import { AsignacionTutor, Tutor } from '../models/tutoria';
import { ServicioGraphQL } from './graphql';

interface DatosDocentes {
  docentes: {
    idDocente: string;
    nombres: string;
    apellidos: string;
    departamento: string;
    activo: boolean;
  }[];
}

interface DatosTutoresAsignados {
  tutoresAsignados: {
    idAsignacion: string;
    idDocente: string;
    nombres: string;
    apellidos: string;
    departamento: string;
    areaRefuerzo: string;
    activo: boolean;
    fechaAsignacion: string;
  }[];
}

interface DatosAsignacion {
  asignarTutores: {
    idAsignacion: string;
    carnetEstudiante: string;
    periodoAcademico: string;
    numeroIntento: number;
    estadoPera: string;
    cantidadTutores: number;
  };
}

export interface SesionTutoria {
  idSesion: number;
  fechaInicio: string;
  fechaFin: string;
  modalidad: string;
  estadoSesion: string;
  cantidadDocentes: number;
  cantidadParticipantes: number;
}

export interface NuevaSesion {
  idAsignacion: number;
  fechaInicio: string;
  fechaFin: string;
  modalidad: string;
  ubicacionEnlace: string;
  idDocenteResponsable: number;
  idDocentes: number[];
  idAsignacionesParticipantes: number[];
}

export interface EstudianteSesion {
  carnet: string;
  nombre: string;
  asignaciones: {
    idAsignacion: number;
    areaRefuerzo: string;
  }[];
}

@Injectable({ providedIn: 'root' })
export class ServicioTutorias {

  private readonly graph = inject(ServicioGraphQL);

  // Consultar docentes y tutores asignados

  consultar(carnet: string) {

    return forkJoin({

      tutores: this.graph.post<DatosDocentes>(
        'query { docentes { idDocente nombres apellidos departamento activo } }',
      ).pipe(map(datos => datos.docentes.filter(d => d.activo).map((d): Tutor => ({
        id: Number(d.idDocente),
        nombre: d.nombres + ' ' + d.apellidos,
        departamento: d.departamento,
        activo: d.activo,
      })))),

      asignaciones: this.graph.post<DatosTutoresAsignados>(
        `query Tutores($carnet: String!) {
          tutoresAsignados(carnet: $carnet) {
            idAsignacion idDocente nombres apellidos departamento
            areaRefuerzo activo fechaAsignacion
          }
        }`,
        { carnet: carnet.trim().toUpperCase() },
      ).pipe(map(datos => datos.tutoresAsignados.map((t): AsignacionTutor => ({
        id: Number(t.idAsignacion),
        carnet: carnet.trim().toUpperCase(),
        tutor: {
          id: Number(t.idDocente),
          nombre: t.nombres + ' ' + t.apellidos,
          departamento: t.departamento,
          activo: t.activo,
        },
        areaRefuerzo: t.areaRefuerzo,
        fechaAsignacion: t.fechaAsignacion,
        responsable: 'Coordinación PERA',
      })))),

    });

  }

  // Asignar un tutor a un estudiante

  asignar(
    carnet: string,
    tutorId: number,
    areaRefuerzo: string,
    periodoAcademico: string
  ) {

    const mutation = `
      mutation AsignarTutores($input: AsignarTutoresInput!) {
        asignarTutores(input: $input) {
          idAsignacion
          carnetEstudiante
          periodoAcademico
          numeroIntento
          estadoPera
          cantidadTutores
        }
      }
    `;

    return this.graph.post<DatosAsignacion>(mutation, {

      input: {
        carnet: carnet.trim().toUpperCase(),
        periodoAcademico: periodoAcademico.trim(),
        tutores: [{
          idDocente: String(tutorId),
          areaRefuerzo: areaRefuerzo.trim()
        }],
      },

    }).pipe(

      switchMap(respuesta => {

        const id = Number(respuesta.asignarTutores.idAsignacion);

        return this.consultar(carnet).pipe(

          map(datos => {

            const asignacion = datos.asignaciones.find(a =>
              a.id === id && a.tutor.id === tutorId
            );

            if (!asignacion) {
              throw new Error(
                'La asignación se guardó, pero no se pudo consultar.'
              );
            }

            return asignacion;

          })

        );

      })

    );

  }

  // Consultar las sesiones de tutoría

  listarSesiones() {

    const query = `
      query {
        sesionesTutoria {
          idSesion
          fechaInicio
          fechaFin
          modalidad
          estadoSesion
          cantidadDocentes
          cantidadParticipantes
        }
      }
    `;

    return this.graph.post<{
      sesionesTutoria: SesionTutoria[]
    }>(query).pipe(
      map(datos => datos.sesionesTutoria)
    );

  }

  // Registrar una nueva sesión

  programarSesion(sesion: NuevaSesion) {

    const mutation = `
      mutation Programar($input: ProgramarSesionInput!) {
        programarSesion(input: $input) {
          idSesion
          fechaInicio
          fechaFin
          modalidad
          estadoSesion
          cantidadDocentes
          cantidadParticipantes
        }
      }
    `;

    return this.graph.post<{
      programarSesion: SesionTutoria
    }>(mutation, {
      input: sesion
    }).pipe(
      map(datos => datos.programarSesion)
    );

  }

  // Publicar una sesión registrada

  publicarSesion(idSesion: number) {

    const mutation = `
      mutation Publicar($idSesion: Long!) {
        publicarSesion(idSesion: $idSesion) {
          idSesion
          estadoSesion
        }
      }
    `;

    return this.graph.post<{
      publicarSesion: {
        idSesion: number;
        estadoSesion: string;
      }
    }>(mutation, {
      idSesion: idSesion
    }).pipe(
      map(datos => datos.publicarSesion)
    );

  }

  // Consultar los estudiantes asignados al docente

  async estudiantesAsignados() {

    const query = `
      query {
        miPerfil {
          idUsuario
        }

        docentes {
          idDocente
          idUsuario
          activo
        }

        estudiantes {
          carnet
          nombres
          apellidos
        }

        asignacionesTutoria {
          idAsignacion
          carnetEstudiante
          estadoPera
        }
      }
    `;

    const datos = await firstValueFrom(
      this.graph.post<{

        miPerfil: {
          idUsuario: number;
        };

        docentes: {
          idDocente: number;
          idUsuario: number;
          activo: boolean;
        }[];

        estudiantes: {
          carnet: string;
          nombres: string;
          apellidos: string;
        }[];

        asignacionesTutoria: {
          idAsignacion: number;
          carnetEstudiante: string;
          estadoPera: string;
        }[];

      }>(query)
    );

    const docente = datos.docentes.find(d =>
      Number(d.idUsuario) === Number(datos.miPerfil.idUsuario)
      && d.activo
    );

    if (!docente) {
      throw new Error('No se encontró el docente activo.');
    }

    const estudiantes: EstudianteSesion[] = [];

    for (const estudiante of datos.estudiantes) {

      const asignaciones = datos.asignacionesTutoria.filter(a =>
        a.carnetEstudiante === estudiante.carnet &&
        a.estadoPera === 'En Curso'
      );

      if (asignaciones.length === 0) {
        continue;
      }

      const resultado = await firstValueFrom(
        this.consultar(estudiante.carnet)
      );

      const propias = resultado.asignaciones.filter(a =>
        a.tutor.id === Number(docente.idDocente) &&
        asignaciones.some(x =>
          Number(x.idAsignacion) === a.id
        )
      );

      if (propias.length > 0) {

        estudiantes.push({

          carnet: estudiante.carnet,

          nombre: estudiante.nombres + ' ' + estudiante.apellidos,

          asignaciones: propias.map(a => ({
            idAsignacion: a.id,
            areaRefuerzo: a.areaRefuerzo
          }))

        });

      }

    }

    return {
      idDocente: Number(docente.idDocente),
      estudiantes
    };

  }

}
