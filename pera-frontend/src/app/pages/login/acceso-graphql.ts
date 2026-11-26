import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { Acceso, PerfilAcceso, Rol } from './acceso';
import { ServicioGraphQL, SesionGraphQL } from '../../services/graphql';

interface RespuestaLogin {
  login: SesionGraphQL;
}

@Injectable()
export class AccesoGraphQL extends Acceso {

  private readonly _perfil = signal<PerfilAcceso | null>(null);

  override readonly perfil = this._perfil.asReadonly();

  constructor(private readonly graph: ServicioGraphQL) {
    super();

    this.restaurarSesion();
  }

  override async ingresar(
    correo: string,
    clave: string
  ): Promise<boolean> {

    const query = `
      mutation Login($correo: String!, $clave: String!) {
        login(correo: $correo, clave: $clave) {
          accessToken
          accessTokenExpiraEn
          refreshToken
          refreshTokenExpiraEn
        }
      }
    `;

    try {

      const respuesta = await firstValueFrom(
        this.graph.post<RespuestaLogin>(query, { correo, clave }, false)
      );

      const sesion = respuesta.login;

      if (!sesion) {
        return false;
      }

      this.graph.guardarSesion(sesion);

      const datos = this.decodificarJwt(sesion.accessToken);

      const rol = this.convertirRol(
        datos?.role ??
        datos?.rol ??
        datos?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      );

      const nombre =
        datos?.name ??
        datos?.nombre ??
        datos?.sub ??
        correo;

      if (!rol) {
        console.error('No se pudo determinar el rol del usuario.');
        this.limpiarSesion();
        return false;
      }

      this._perfil.set({
        nombre: String(nombre),
        rol
      });

      return true;

    } catch (error) {
      console.error('Error de login:', error);
      return false;
    }
  }

  override async salir(): Promise<void> {
    const token = localStorage.getItem('pera_refresh_token');

    if (token) {
      try {
        await firstValueFrom(this.graph.post(
          'mutation Logout($token: String!) { logout(token: $token) }',
          { token },
          false
        ));
      } catch {
        // Se cierra la sesión local aunque el backend no responda.
      }
    }

    this.limpiarSesion();
  }

  private limpiarSesion(): void {
    this.graph.limpiarSesion();
    this._perfil.set(null);
  }

  private restaurarSesion(): void {

    const token = localStorage.getItem('pera_access_token');

    if (!token) {
      return;
    }

    const datos = this.decodificarJwt(token);

    if (!datos) {
      this.limpiarSesion();
      return;
    }

    const rol = this.convertirRol(
      datos?.role ??
      datos?.rol ??
      datos?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    );

    if (!rol) {
      this.limpiarSesion();
      return;
    }

    this._perfil.set({
      nombre: String(
        datos?.name ??
        datos?.nombre ??
        datos?.sub ??
        'Usuario'
      ),
      rol
    });
  }

  private decodificarJwt(token: string): any {

    try {

      const payload = token.split('.')[1];

      const normalizado = payload
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const json = decodeURIComponent(
        atob(normalizado)
          .split('')
          .map(
            c =>
              '%' +
              ('00' + c.charCodeAt(0).toString(16)).slice(-2)
          )
          .join('')
      );

      return JSON.parse(json);

    } catch {
      return null;
    }
  }

  private convertirRol(valor: unknown): Rol | null {

    switch (valor) {

      case 'ADMIN_ACADEMICA':
      case 'AdministracionAcademica':
        return 'AdministracionAcademica';

      case 'COORDINACION_PERA':
      case 'CoordinacionPERA':
        return 'CoordinacionPERA';

      case 'JEFATURA':
      case 'Jefatura':
        return 'Jefatura';

      case 'DOCENTE':
      case 'DocenteTutor':
        return 'DocenteTutor';

      case 'ESTUDIANTE':
      case 'Estudiante':
        return 'Estudiante';

      default:
        return null;
    }
  }
}
