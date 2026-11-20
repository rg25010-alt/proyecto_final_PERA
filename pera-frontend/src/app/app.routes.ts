import { Routes } from '@angular/router';
import { PaginaExpedientes } from './pages/expedientes/expedientes';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'expedientes' },
  { path: 'expedientes', component: PaginaExpedientes, title: 'Expedientes estudiantiles | PERA' },
];
