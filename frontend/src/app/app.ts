import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly anio = new Date().getFullYear();

  /**
   * Lleva el foco al contenido principal. Se hace por código porque con `<base href="/">`
   * un enlace "#contenido" navegaría a la raíz del sitio en lugar de quedarse en la página.
   */
  protected saltarAlContenido(evento: Event): void {
    evento.preventDefault();
    document.getElementById('contenido')?.focus();
  }

  protected cerrarSesion(): void {
    // Primero se navega (el guard puede pedir confirmar una evaluación sin guardar) y luego se cierra.
    void this.router.navigate(['/analista/login']).then((navego) => {
      if (navego) {
        this.auth.cerrarSesion();
      }
    });
  }
}
