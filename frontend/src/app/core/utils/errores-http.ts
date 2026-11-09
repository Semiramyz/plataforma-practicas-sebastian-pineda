import { HttpErrorResponse } from '@angular/common/http';

/** Respuesta de error de la API (ProblemDetails, RFC 9457). */
interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface ErrorApi {
  mensaje: string;
  /** Errores de validación por campo, con la clave en camelCase. */
  campos: Record<string, string>;
}

export function interpretarError(error: unknown): ErrorApi {
  if (!(error instanceof HttpErrorResponse)) {
    return { mensaje: 'Ocurrió un error inesperado.', campos: {} };
  }

  if (error.status === 0) {
    return {
      mensaje: 'No fue posible conectar con el servidor. Verifica que la API esté en ejecución.',
      campos: {},
    };
  }

  const problema = (typeof error.error === 'object' ? error.error : null) as ProblemDetails | null;
  const campos: Record<string, string> = {};
  for (const [campo, mensajes] of Object.entries(problema?.errors ?? {})) {
    if (mensajes.length > 0) {
      campos[campo.charAt(0).toLowerCase() + campo.slice(1)] = mensajes[0];
    }
  }

  const mensaje =
    error.status === 400 && Object.keys(campos).length > 0
      ? 'Revisa los campos marcados.'
      : (problema?.detail ?? problema?.title ?? 'Ocurrió un error procesando la solicitud.');

  return { mensaje, campos };
}

export function formatearTamano(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(0)} KB`;
  }
  const megas = bytes / (1024 * 1024);
  return `${Number.isInteger(megas) ? megas : megas.toFixed(1)} MB`;
}
