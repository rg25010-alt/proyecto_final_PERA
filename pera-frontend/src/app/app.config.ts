import { Acceso } from './pages/login/acceso';
import { AccesoGraphQL } from './pages/login/acceso-graphql';
import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),

    { provide: Acceso, useClass: AccesoGraphQL },
  ]

};
