
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Acceso } from '../pages/login/acceso';

export const authGuard: CanActivateFn = () => {

  const acceso = inject(Acceso);
  const router = inject(Router);

  const perfil = acceso.perfil();
  const token = localStorage.getItem('pera_access_token');
  const refresh = localStorage.getItem('pera_refresh_token');

  const expira = Date.parse(
    localStorage.getItem('pera_access_expira') || ''
  );

  const refreshExpira = Date.parse(
    localStorage.getItem('pera_refresh_expira') || ''
  );

  if (perfil && token) {

    if (expira > Date.now()) {
      return true;
    }

    if (refresh && refreshExpira > Date.now()) {
      return true;
    }

  }

  return router.createUrlTree(['/login']);

};
