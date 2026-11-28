import { Login } from './pages/login/login';
import { Routes } from '@angular/router';
import { PaginaExpedientes } from './pages/expedientes/expedientes';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: Login, title: 'Inicio de sesión | PERA' },
  { path: 'expedientes', component: PaginaExpedientes, title: 'Expedientes estudiantiles | PERA' },
];
