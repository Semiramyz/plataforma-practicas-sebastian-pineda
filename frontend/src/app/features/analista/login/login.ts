import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { interpretarError } from '../../../core/utils/errores-http';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Ruta a la que se vuelve después de iniciar sesión (query param). */
  readonly volver = input<string>();
  /** Motivo por el que se pidió iniciar sesión (query param). */
  readonly motivo = input<string>();

  protected readonly formulario = inject(NonNullableFormBuilder).group({
    usuario: ['', Validators.required],
    contrasena: ['', Validators.required],
  });

  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected enviar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.error.set('Escribe tu usuario y contraseña.');
      return;
    }

    const { usuario, contrasena } = this.formulario.getRawValue();
    this.enviando.set(true);
    this.error.set(null);

    this.auth.iniciarSesion(usuario, contrasena).subscribe({
      next: () => void this.router.navigateByUrl(this.destinoSeguro()),
      error: (error: unknown) => {
        this.enviando.set(false);
        this.error.set(interpretarError(error).mensaje);
        this.formulario.controls.contrasena.reset();
      },
    });
  }

  /** Solo permite redirigir a rutas internas del panel. */
  private destinoSeguro(): string {
    const destino = this.volver();
    return destino?.startsWith('/analista') && !destino.startsWith('/analista/login')
      ? destino
      : '/analista';
  }
}
