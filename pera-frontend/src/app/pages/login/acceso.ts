import { Signal } from '@angular/core';

export type Rol = 'AdministracionAcademica' | 'CoordinacionPERA' | 'Jefatura' | 'DocenteTutor' | 'Estudiante';

export interface PerfilAcceso {
  nombre: string;
  rol: Rol;
}

export abstract class Acceso {
  abstract readonly perfil: Signal<PerfilAcceso | null>;
  abstract ingresar(correo: string, clave: string): Promise<boolean>;
  abstract salir(): Promise<void>;
}
