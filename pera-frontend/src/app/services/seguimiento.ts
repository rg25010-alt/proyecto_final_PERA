
import { Injectable, inject } from '@angular/core';
import { firstValueFrom, map } from 'rxjs';
import { ServicioGraphQL } from './graphql';
import { ServicioTutorias } from './tutorias';

export interface TutoriaSeguimiento {
  id: number;
  idSesion: number;
  carnet: string;
  estudiante: string;
  materia: string;
  fecha: string;
  asistencia: string;
  observaciones: string | null;
  nota: string;
  acuerdos: string;
  calificacion: number | null;
}

interface DatosAsistencias {
  asistencias: {
    idParticipacion: number;
    carnetEstudiante: string;
    asistio: boolean | null;
    observaciones: string | null;
  }[];
}

interface DatosEstudiantes {
  estudiantes: {
    carnet: string;
    nombres: string;
    apellidos: string;
  }[];
}

export interface Bitacora {
  idBitacora: number;
  idParticipacion: number;
  descripcion: string;
  calificacion: number | null;
  acuerdos: string | null;
  fechaRegistro: string;
}

@Injectable({ providedIn: 'root' })
export class ServicioSeguimiento {

  private readonly graph = inject(ServicioGraphQL);
  private readonly servicioTutorias = inject(ServicioTutorias);

  // Consultar las tutorías y sus participantes

  async listar() {

    const sesiones = await firstValueFrom(
      this.servicioTutorias.listarSesiones()
    );

    const datos = await firstValueFrom(
      this.graph.post<DatosEstudiantes>(`
        query {
          estudiantes {
            carnet
            nombres
            apellidos
          }
        }
      `)
    );

    const tutorias: TutoriaSeguimiento[] = [];

    for (const sesion of sesiones) {

      if (sesion.estadoSesion !== 'Programada' &&
          sesion.estadoSesion !== 'Finalizada') {
        continue;
      }

      const resultado = await firstValueFrom(
        this.graph.post<DatosAsistencias>(`
          query Asistencias($idSesion: Long!) {
            asistencias(idSesion: $idSesion) {
              idParticipacion
              carnetEstudiante
              asistio
              observaciones
            }
          }
        `, {
          idSesion: Number(sesion.idSesion)
        })
      );

      for (const participante of resultado.asistencias) {

        const estudiante = datos.estudiantes.find(e =>
          e.carnet === participante.carnetEstudiante
        );

        let asistencia = 'Pendiente';

        if (participante.asistio === true) {
          asistencia = 'Asistió';
        }

        if (participante.asistio === false) {
          asistencia = 'No asistió';
        }

        const bitacoras = await firstValueFrom(
          this.consultarBitacoras(
            Number(participante.idParticipacion)
          )
        );

        const ultima = bitacoras[0];

        tutorias.push({

          id: Number(participante.idParticipacion),

          idSesion: Number(sesion.idSesion),

          carnet: participante.carnetEstudiante,

          estudiante: estudiante
            ? estudiante.nombres + ' ' + estudiante.apellidos
            : participante.carnetEstudiante,

          materia: 'Tutoría #' + sesion.idSesion,

          fecha: sesion.fechaInicio,

          asistencia: asistencia,

          observaciones: participante.observaciones,

          nota: ultima?.descripcion ?? '',

          acuerdos: ultima?.acuerdos ?? '',

          calificacion: ultima?.calificacion ?? null

        });

      }

    }

    return tutorias;

  }

  // Registrar o actualizar la asistencia

  registrarAsistencia(
    idParticipacion: number,
    asistio: boolean,
    observaciones: string | null
  ) {

    const mutation = `
      mutation Registrar($input: RegistrarAsistenciaInput!) {
        registrarAsistencia(input: $input) {
          idAsistencia
          idParticipacion
          asistio
          fechaRegistro
        }
      }
    `;

    return this.graph.post<{
      registrarAsistencia: {
        idAsistencia: number;
        idParticipacion: number;
        asistio: boolean;
        fechaRegistro: string;
      }
    }>(mutation, {

      input: {
        idParticipacion,
        asistio,
        observaciones
      }

    }).pipe(
      map(datos => datos.registrarAsistencia)
    );

  }

  // Consultar las bitácoras de un participante

  consultarBitacoras(idParticipacion: number) {

    const query = `
      query Bitacoras($idParticipacion: Long!) {
        bitacoras(idParticipacion: $idParticipacion) {
          idBitacora
          idParticipacion
          descripcion
          calificacion
          acuerdos
          fechaRegistro
        }
      }
    `;

    return this.graph.post<{
      bitacoras: Bitacora[]
    }>(query, {

      idParticipacion

    }).pipe(
      map(datos => datos.bitacoras)
    );

  }

  // Registrar una nueva bitácora

  registrarBitacora(
    idParticipacion: number,
    descripcion: string,
    calificacion: number | null,
    acuerdos: string
  ) {

    const mutation = `
      mutation Registrar($input: RegistrarBitacoraInput!) {
        registrarBitacora(input: $input) {
          idBitacora
          idParticipacion
          descripcion
          calificacion
          fechaRegistro
        }
      }
    `;

    return this.graph.post<{
      registrarBitacora: {
        idBitacora: number;
        idParticipacion: number;
        descripcion: string;
        calificacion: number | null;
        fechaRegistro: string;
      }
    }>(mutation, {

      input: {
        idParticipacion,
        descripcion: descripcion.trim(),
        calificacion,
        acuerdos: acuerdos.trim() || null
      }

    }).pipe(
      map(datos => datos.registrarBitacora)
    );

  }

}
