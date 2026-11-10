import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { Sesion } from '../models/sesion.model';

const CLAVE_SESION = 'practicas.sesion';

/**
 * Maneja la sesión del analista.
 * - El token se guarda en sessionStorage: la sesión termina al cerrar la pestaña.
 * - Al expirar el token, la sesión se cierra sola y se vuelve al inicio de sesión.
 * - Al cerrar sesión se avisa a la API para que el token deje de ser válido de inmediato.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sesion = signal<Sesion | null>(leerSesionGuardada());
  private temporizador: ReturnType<typeof setTimeout> | undefined;

  readonly sesionActual = this.sesion.asReadonly();
  readonly nombre = computed(() => this.sesion()?.nombre ?? null);

  constructor() {
    const sesion = this.sesion();
    if (sesion) {
      this.programarExpiracion(sesion);
    }
  }

  estaAutenticado(): boolean {
    const sesion = this.sesion();
    if (!sesion) {
      return false;
    }
    if (new Date(sesion.expiraUtc).getTime() <= Date.now()) {
      this.limpiar();
      return false;
    }
    return true;
  }

  token(): string | null {
    return this.estaAutenticado() ? this.sesion()!.token : null;
  }

  iniciarSesion(usuario: string, contrasena: string): Observable<Sesion> {
    return this.http
      .post<Sesion>('/api/auth/login', { usuario, contrasena })
      .pipe(tap((sesion) => this.guardar(sesion)));
  }

  /** Revoca el token en la API (si sigue vigente) y borra la sesión local. */
  cerrarSesion(): void {
    if (this.token()) {
      // El interceptor agrega el token; el resultado no cambia lo que se hace en el navegador.
      this.http.post('/api/auth/logout', null).subscribe({ error: () => undefined });
    }
    this.limpiar();
  }

  /** Borra la sesión local sin llamar a la API (token vencido o rechazado). */
  limpiar(): void {
    clearTimeout(this.temporizador);
    this.sesion.set(null);
    sessionStorage.removeItem(CLAVE_SESION);
  }

  private guardar(sesion: Sesion): void {
    this.sesion.set(sesion);
    sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
    this.programarExpiracion(sesion);
  }

  private programarExpiracion(sesion: Sesion): void {
    clearTimeout(this.temporizador);
    const restante = new Date(sesion.expiraUtc).getTime() - Date.now();
    this.temporizador = setTimeout(
      () => {
        this.limpiar();
        void this.router.navigate(['/analista/login'], {
          queryParams: { motivo: 'sesion-expirada' },
        });
      },
      Math.max(0, Math.min(restante, 2_147_000_000)),
    );
  }
}

function leerSesionGuardada(): Sesion | null {
  try {
    const valor = sessionStorage.getItem(CLAVE_SESION);
    return valor ? (JSON.parse(valor) as Sesion) : null;
  } catch {
    return null;
  }
}
