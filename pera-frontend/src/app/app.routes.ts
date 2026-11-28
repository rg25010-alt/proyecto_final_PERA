
import { Routes } from '@angular/router';

import { Login } from './pages/login/login';
import { Inicio } from './pages/inicio/inicio';
import { PaginaExpedientes } from './pages/expedientes/expedientes';
import { Estudiantes } from './pages/estudiantes/estudiantes';
import { ExpedienteFormulario } from './pages/expediente-formulario/expediente-formulario';
import { ExpedienteDetalle } from './pages/expediente-detalle/expediente-detalle';
import { AsignacionTutores } from './pages/asignacion-tutores/asignacion-tutores';
import { Calendario } from './pages/calendario/calendario';
import { SesionFormulario } from './pages/sesion-formulario/sesion-formulario';
import { Seguimiento } from './pages/seguimiento/seguimiento';
import { Notificaciones } from './pages/notificaciones/notificaciones';
import { Docentes } from './pages/docentes/docentes';
import { MiPera } from './pages/mi-pera/mi-pera';
import { Reportes } from './pages/reportes/reportes';
import { HabilitacionEgreso } from './pages/habilitacion-egreso/habilitacion-egreso';


import { authGuard } from './guards/auth.guard';
import { rolGuard } from './guards/rol.guard';

export const routes: Routes = [

  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },

  {
    path: 'login',
    component: Login,
    title: 'Inicio de sesión | PERA'
  },

  {
    path: 'inicio',
    component: Inicio,
    title: 'Inicio | PERA',
    canActivate: [authGuard]
  },

  {
    path: 'expedientes',
    component: PaginaExpedientes,
    title: 'Expedientes estudiantiles | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['AdministracionAcademica']
    }
  },

  {
    path: 'estudiantes',
    component: Estudiantes,
    title: 'Estudiantes | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: [
        'AdministracionAcademica',
        'CoordinacionPERA',
        'Jefatura',
        'DocenteTutor'
      ]
    }
  },

  {
    path: 'expediente-formulario',
    component: ExpedienteFormulario,
    title: 'Registrar expediente | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['AdministracionAcademica']
    }
  },

  {
    path: 'expediente-detalle/:carnet',
    component: ExpedienteDetalle,
    title: 'Detalle de expediente | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: [
        'AdministracionAcademica',
        'CoordinacionPERA',
        'Jefatura',
        'DocenteTutor'
      ]
    }
  },

  {
    path: 'asignacion-tutores',
    component: AsignacionTutores,
    title: 'Asignación de tutores | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['CoordinacionPERA']
    }
  },

  {
    path: 'calendario',
    component: Calendario,
    title: 'Calendario | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['DocenteTutor', 'Estudiante']
    }
  },

  {
    path: 'sesion-formulario',
    component: SesionFormulario,
    title: 'Programar sesión | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['DocenteTutor']
    }
  },

  {
    path: 'seguimiento',
    component: Seguimiento,
    title: 'Seguimiento de tutorías | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['DocenteTutor']
    }
  },

  {
    path: 'notificaciones',
    component: Notificaciones,
    title: 'Notificaciones | PERA',
    canActivate: [authGuard]
  },

  {
    path: 'docentes',
    component: Docentes,
    title: 'Docentes | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['Jefatura']
    }
  },

  {
    path: 'mi-pera',
    component: MiPera,
    title: 'Mi PERA | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['Estudiante']
    }
  },

  {
    path: 'reportes',
    component: Reportes,
    title: 'Reportes | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['Jefatura']
    }
  },

  {
    path: 'habilitacion-egreso',
    component: HabilitacionEgreso,
    title: 'Habilitación de egreso | PERA',
    canActivate: [authGuard, rolGuard],
    data: {
      roles: ['AdministracionAcademica', 'Jefatura']
    }
  }

];

