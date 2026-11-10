import { CanDeactivateFn } from '@angular/router';

/** Pantallas con un formulario que se puede perder al salir. */
export interface ConCambiosSinGuardar {
  tieneCambiosSinGuardar(): boolean;
}

/** Pide confirmación antes de salir de una evaluación empezada y no guardada. */
export const cambiosSinGuardarGuard: CanDeactivateFn<ConCambiosSinGuardar> = (componente) =>
  !componente.tieneCambiosSinGuardar() ||
  window.confirm('Tienes una evaluación sin guardar. ¿Salir y descartar los cambios?');
