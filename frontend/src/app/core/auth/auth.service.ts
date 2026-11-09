import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { Sesion } from '../models/sesion.model';

const CLAVE_SESION = 'practicas.sesion';

/**
 * Maneja la sesión del analista. El token se guarda en sessionStorage para que
 * la sesión termine al cerrar la pestaña del navegador.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sesion = signal<Sesion | null>(leerSesionGuardada());

  readonly sesionActual = this.sesion.asReadonly();
  readonly nombre = computed(() => this.sesion()?.nombre ?? null);

  estaAutenticado(): boolean {
    const sesion = this.sesion();
    if (!sesion) {
      return false;
    }
    if (new Date(sesion.expiraUtc).getTime() <= Date.now()) {
      this.cerrarSesion();
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

  cerrarSesion(): void {
    this.sesion.set(null);
    sessionStorage.removeItem(CLAVE_SESION);
  }

  private guardar(sesion: Sesion): void {
    this.sesion.set(sesion);
    sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
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
