
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Acceso, Rol } from '../pages/login/acceso';

export const rolGuard: CanActivateFn = (route) => {

  const acceso = inject(Acceso);
  const router = inject(Router);

  const perfil = acceso.perfil();
  const rolesPermitidos: Rol[] = route.data['roles'] || [];

  if (!perfil) {
    return router.createUrlTree(['/login']);
  }

  if (rolesPermitidos.includes(perfil.rol)) {
    return true;
  }

  return router.createUrlTree(['/inicio']);

};
