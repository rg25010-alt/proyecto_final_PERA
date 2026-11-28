import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { Carrera, Expediente, NuevoExpediente } from '../models/expediente';
import { ServicioGraphQL } from './graphql';

interface DatosCarreras {
  carreras: { idCarrera: number; nombreCarrera: string }[];
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
      const estudiante = datos.estudiantes.find(e => e.carnet === filtro);
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
    const apellidos = datos.apellidos?.trim() || nombre.split(' ').slice(1).join(' ').trim();
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
