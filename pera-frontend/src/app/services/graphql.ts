import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, defer, map, of, switchMap } from 'rxjs';

export interface SesionGraphQL {
  accessToken: string;
  accessTokenExpiraEn: string;
  refreshToken: string;
  refreshTokenExpiraEn: string;
}

interface RespuestaGraphQL<T> {
  data?: T;
  errors?: { message: string }[];
}

@Injectable({ providedIn: 'root' })
export class ServicioGraphQL {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/graphql';

  post<T>(query: string, variables: Record<string, unknown> = {}, conAuth = true): Observable<T> {
    return defer(() => conAuth ? this.renovarSiNecesario() : of(void 0)).pipe(
      switchMap(() => {
        const token = localStorage.getItem('pera_access_token');
        return this.raw<T>(query, variables, conAuth ? token ?? undefined : undefined);
      }),
      map(respuesta => this.validar(respuesta)),
    );
  }

  guardarSesion(sesion: SesionGraphQL): void {
    localStorage.setItem('pera_access_token', sesion.accessToken);
    localStorage.setItem('pera_refresh_token', sesion.refreshToken);
    localStorage.setItem('pera_access_expira', sesion.accessTokenExpiraEn);
    localStorage.setItem('pera_refresh_expira', sesion.refreshTokenExpiraEn);
  }

  limpiarSesion(): void {
    localStorage.removeItem('pera_access_token');
    localStorage.removeItem('pera_refresh_token');
    localStorage.removeItem('pera_access_expira');
    localStorage.removeItem('pera_refresh_expira');
  }

  private raw<T>(query: string, variables: Record<string, unknown>, token?: string) {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = 'Bearer ' + token;

    return this.http.post<RespuestaGraphQL<T>>(
      this.endpoint,
      { query, variables },
      { headers },
    );
  }

  private validar<T>(respuesta: RespuestaGraphQL<T>): T {
    if (respuesta.errors?.length) {
      throw new Error(respuesta.errors.map(e => e.message).join(' | '));
    }
    if (!respuesta.data) {
      throw new Error('El servidor no devolvió datos.');
    }
    return respuesta.data;
  }

  private renovarSiNecesario(): Observable<void> {
    const expira = localStorage.getItem('pera_access_expira');
    const refresh = localStorage.getItem('pera_refresh_token');

    if (!expira || !refresh) return of(void 0);

    const restante = Date.parse(expira) - Date.now();
    if (!Number.isFinite(restante) || restante > 60_000) return of(void 0);

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

    return this.raw<{ refreshToken: SesionGraphQL }>(query, { token: refresh }).pipe(
      map(respuesta => {
        if (respuesta.errors?.length || !respuesta.data?.refreshToken) {
          throw new Error('No se pudo renovar la sesión.');
        }
        this.guardarSesion(respuesta.data.refreshToken);
      }),
      catchError(() => {
        this.limpiarSesion();
        return of(void 0);
      }),
    );
  }
}
