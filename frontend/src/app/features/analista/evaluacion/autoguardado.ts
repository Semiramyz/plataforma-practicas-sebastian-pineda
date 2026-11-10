import { InjectionToken } from '@angular/core';

/**
 * Milisegundos que se espera tras el último cambio antes de guardar el borrador en la base de
 * datos. Agrupa las pulsaciones de teclado en una sola solicitud. Las pruebas lo ponen en 0.
 */
export const RETARDO_AUTOGUARDADO_MS = new InjectionToken<number>('RETARDO_AUTOGUARDADO_MS', {
  factory: () => 800,
});

/** Estado del guardado automático que se muestra al analista. */
export type EstadoAutoguardado = 'inactivo' | 'pendiente' | 'guardando' | 'guardado' | 'error';
