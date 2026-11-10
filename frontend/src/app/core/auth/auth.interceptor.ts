import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Adjunta el JWT del analista a las llamadas a la API y, si el servidor responde
 * 401 (token vencido o inválido), cierra la sesión y lleva al inicio de sesión.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token();

  const solicitud =
    token && req.url.startsWith('/api/')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(solicitud).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !req.url.startsWith('/api/auth/')
      ) {
        // El token ya no sirve: se borra localmente sin volver a llamar a la API.
        auth.limpiar();
        void router.navigate(['/analista/login'], {
          queryParams: { volver: router.url, motivo: 'sesion-expirada' },
        });
      }
      return throwError(() => error);
    }),
  );
};
