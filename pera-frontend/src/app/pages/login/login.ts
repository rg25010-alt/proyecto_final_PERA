import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Acceso } from './acceso';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly acceso = inject(Acceso);
  private readonly router = inject(Router);
  readonly error = signal('');
  readonly enviando = signal(false);

  async ingresar(evento: Event, correo: HTMLInputElement, clave: HTMLInputElement) {
    evento.preventDefault();
    if (this.enviando()) return;
    this.enviando.set(true);
    this.error.set('');
    try {
      const valido = await this.acceso.ingresar(correo.value.trim(), clave.value);
      if (valido) {
        await this.router.navigateByUrl('/inicio', { replaceUrl: true });
      } else {
        this.error.set('Revisá el correo y la contraseña.');
        clave.focus();
      }
    } catch {
      this.error.set('No se pudo iniciar la sesión. Volvé a intentarlo.');
    } finally {
      clave.value = '';
      this.enviando.set(false);
    }
  }
}
