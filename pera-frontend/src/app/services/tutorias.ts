import { Injectable, inject } from '@angular/core';
import { forkJoin, map, switchMap } from 'rxjs';
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

@Injectable({ providedIn: 'root' })
export class ServicioTutorias {
  private readonly graph = inject(ServicioGraphQL);

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

  asignar(carnet: string, tutorId: number, areaRefuerzo: string, periodoAcademico: string) {
    const mutation = `
      mutation AsignarTutores($input: AsignarTutoresInput!) {
        asignarTutores(input: $input) {
          idAsignacion carnetEstudiante periodoAcademico
          numeroIntento estadoPera cantidadTutores
        }
      }
    `;

    return this.graph.post<DatosAsignacion>(mutation, {
      input: {
        carnet: carnet.trim().toUpperCase(),
        periodoAcademico: periodoAcademico.trim(),
        tutores: [{ idDocente: String(tutorId), areaRefuerzo: areaRefuerzo.trim() }],
      },
    }).pipe(
      switchMap(() => this.consultar(carnet)),
      map(datos => datos.asignaciones.find(a =>
        a.tutor.id === tutorId && a.areaRefuerzo === areaRefuerzo.trim()
      ) ?? datos.asignaciones.at(-1)!),
    );
  }
}
