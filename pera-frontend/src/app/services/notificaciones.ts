
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { ServicioGraphQL } from './graphql';

interface NotificacionServidor {
  idNotificacion: number;
  tipo: string;
  mensaje: string;
  fechaProgramada: string;
  estadoEnvio: string;
}

interface DatosNotificaciones {
  miPerfil: {
    idUsuario: number;
  };
  notificaciones: NotificacionServidor[];
}

export interface Aviso {
  id: number;
  titulo: string;
  mensaje: string;
  fecha: string;
  tipo: string;
  estadoEnvio: string;
  destinatario: string;
  leida: boolean;
}

@Injectable({ providedIn: 'root' })
export class ServicioNotificaciones {

  private readonly graph = inject(ServicioGraphQL);

  private claveLectura = '';

  listar(rol: string) {

    const query = `
      query {
        miPerfil {
          idUsuario
        }

        notificaciones {
          idNotificacion
          tipo
          mensaje
          fechaProgramada
          estadoEnvio
        }
      }
    `;

    return this.graph.post<DatosNotificaciones>(query).pipe(

      map(datos => {

        this.claveLectura =
          'pera_notificaciones_leidas_' +
          datos.miPerfil.idUsuario;

        const leidas = this.obtenerLeidas();

        return datos.notificaciones.map((n): Aviso => ({

          id: Number(n.idNotificacion),
          titulo: n.tipo,
          mensaje: n.mensaje,
          fecha: new Date(n.fechaProgramada)
            .toLocaleDateString('es-SV'),
          tipo: n.tipo,
          estadoEnvio: n.estadoEnvio,
          destinatario: rol,
          leida: leidas.includes(Number(n.idNotificacion))

        }));

      })

    );

  }

  private obtenerLeidas(): number[] {

    if (!this.claveLectura) {
      return [];
    }

    try {

      const datos = JSON.parse(
        localStorage.getItem(this.claveLectura) || '[]'
      );

      return Array.isArray(datos)
        ? datos.filter(id => typeof id === 'number')
        : [];

    } catch {

      return [];

    }

  }

  marcarLeidas(ids: number[]): void {

    if (!this.claveLectura) {
      return;
    }

    const leidas = this.obtenerLeidas();

    for (const id of ids) {

      if (!leidas.includes(id)) {
        leidas.push(id);
      }

    }

    localStorage.setItem(
      this.claveLectura,
      JSON.stringify(leidas)
    );

  }

}
