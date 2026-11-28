import { Acceso } from './pages/login/acceso';
import { AccesoGraphQL } from './pages/login/acceso-graphql';
import { provideHttpClient } from '@angular/common/http';
import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { ListadoAutorizado } from './pages/estudiantes/listado-autorizado';
import { ConsultaDetalle } from './pages/expediente-detalle/consulta-detalle';
import {
  ListadoAutorizadoGraphQL,
  ConsultaDetalleGraphQL
} from './services/expedientes';
export const appConfig: ApplicationConfig = {

  providers: [

    provideBrowserGlobalErrorListeners(),

    provideRouter(routes),

    provideHttpClient(),

    { provide: Acceso, useClass: AccesoGraphQL },

    {
      provide: ListadoAutorizado,
      useClass: ListadoAutorizadoGraphQL
    },

    {
      provide: ConsultaDetalle,
      useClass: ConsultaDetalleGraphQL
    },

  ]

};
