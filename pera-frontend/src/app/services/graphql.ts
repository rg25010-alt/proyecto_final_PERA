
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import {
  Observable,
  catchError,
  defer,
  map,
  of,
  switchMap,
  finalize,
  shareReplay,
  throwError
} from 'rxjs';

export interface SesionGraphQL {
  accessToken: string;
  accessTokenExpiraEn: string;
  refreshToken: string;
  refreshTokenExpiraEn: string;
}

interface RespuestaGraphQL<T> {
  data?: T;
  errors?: {
    message: string;
  }[];
}

@Injectable({ providedIn: 'root' })
export class ServicioGraphQL {

  private readonly http = inject(HttpClient);

  private readonly endpoint = '/graphql';

  private renovacion: Observable<void> | null = null;

  // Enviar peticiones al backend

  post<T>(
    query: string,
    variables: Record<string, unknown> = {},
    conAuth = true
  ): Observable<T> {

    return defer(() =>

      conAuth
        ? this.renovarSiNecesario()
        : of(void 0)

    ).pipe(

      switchMap(() => {

        const token = localStorage.getItem(
          'pera_access_token'
        );

        return this.raw<T>(
          query,
          variables,
          conAuth ? token ?? undefined : undefined
        );

      }),

      map(respuesta => this.validar(respuesta))

    );

  }

  // Guardar los tokens de la sesion

  guardarSesion(sesion: SesionGraphQL): void {

    localStorage.setItem(
      'pera_access_token',
      sesion.accessToken
    );

    localStorage.setItem(
      'pera_refresh_token',
      sesion.refreshToken
    );

    localStorage.setItem(
      'pera_access_expira',
      sesion.accessTokenExpiraEn
    );

    localStorage.setItem(
      'pera_refresh_expira',
      sesion.refreshTokenExpiraEn
    );

  }

  // Eliminar los datos de la sesion

  limpiarSesion(): void {

    localStorage.removeItem('pera_access_token');

    localStorage.removeItem('pera_refresh_token');

    localStorage.removeItem('pera_access_expira');

    localStorage.removeItem('pera_refresh_expira');

  }

  // Construir y enviar la peticion HTTP

  private raw<T>(
    query: string,
    variables: Record<string, unknown>,
    token?: string
  ) {

    const headers: Record<string, string> = {

      'Content-Type': 'application/json'

    };

    if (token) {

      headers['Authorization'] = 'Bearer ' + token;

    }

    return this.http.post<RespuestaGraphQL<T>>(

      this.endpoint,

      { query, variables },

      { headers }

    );

  }

  // Validar la respuesta de GraphQL

  private validar<T>(respuesta: RespuestaGraphQL<T>): T {

    if (respuesta.errors?.length) {

      throw new Error(

        respuesta.errors.map(
          e => e.message
        ).join(' | ')

      );

    }

    if (!respuesta.data) {

      throw new Error(
        'El servidor no devolvió datos.'
      );

    }

    return respuesta.data;

  }

  // Renovar el token cuando este proximo a vencer

  private renovarSiNecesario(): Observable<void> {

    if (this.renovacion) {

      return this.renovacion;

    }

    const token = localStorage.getItem(
      'pera_access_token'
    );

    const expira = localStorage.getItem(
      'pera_access_expira'
    );

    const refresh = localStorage.getItem(
      'pera_refresh_token'
    );

    const restante =
      Date.parse(expira || '') - Date.now();

    if (
      token &&
      Number.isFinite(restante) &&
      restante > 60000
    ) {

      return of(void 0);

    }

    if (!refresh) {

      this.limpiarSesion();

      window.location.replace('/login');

      return throwError(() =>
        new Error('La sesión ha finalizado.')
      );

    }

    const query = `

      mutation Refresh($token: String!) {

        refreshToken(token: $token) {

          accessToken

          accessTokenExpiraEn

          refreshToken

          refreshTokenExpiraEn

        }

      }

    `;

    this.renovacion = this.raw<{

      refreshToken: SesionGraphQL

    }>(query, {

      token: refresh

    }).pipe(

      map(respuesta => {

        const datos = this.validar(respuesta);

        if (!datos.refreshToken) {

          throw new Error(
            'No se pudo renovar la sesión.'
          );

        }

        if (
          localStorage.getItem('pera_refresh_token')
          !== refresh
        ) {

          throw new Error(
            'La sesión cambió durante la renovación.'
          );

        }

        this.guardarSesion(datos.refreshToken);

      }),

      catchError(error => {

        if (
          localStorage.getItem('pera_refresh_token')
          === refresh
        ) {

          this.limpiarSesion();

          window.location.replace('/login');

        }

        return throwError(() => error);

      }),

      finalize(() => {

        this.renovacion = null;

      }),

      shareReplay(1)

    );

    return this.renovacion;

  }

}
