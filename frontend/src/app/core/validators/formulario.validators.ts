import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const TAMANO_MAXIMO_HOJA_DE_VIDA = 5 * 1024 * 1024;

/** Rechaza textos que solo contienen espacios. */
export const noVacio: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = control.value;
  return typeof valor === 'string' && valor.length > 0 && valor.trim().length === 0
    ? { required: true }
    : null;
};

/** Valida que el archivo seleccionado sea un PDF y no supere el tamaño máximo. */
export function archivoPdf(tamanoMaximoBytes = TAMANO_MAXIMO_HOJA_DE_VIDA): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const archivo = control.value as File | null;
    if (!archivo) {
      return null;
    }

    const esPdf =
      archivo.name.toLowerCase().endsWith('.pdf') &&
      (archivo.type === '' || archivo.type === 'application/pdf');
    if (!esPdf) {
      return { tipoArchivo: true };
    }

    if (archivo.size === 0) {
      return { archivoVacio: true };
    }

    return archivo.size > tamanoMaximoBytes
      ? { tamanoArchivo: { maximo: tamanoMaximoBytes, actual: archivo.size } }
      : null;
  };
}
