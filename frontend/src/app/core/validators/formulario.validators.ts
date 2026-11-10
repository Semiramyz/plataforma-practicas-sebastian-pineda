import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const TAMANO_MAXIMO_HOJA_DE_VIDA = 5 * 1024 * 1024;

/** Rechaza textos que solo contienen espacios. */
export const noVacio: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = control.value;
  return typeof valor === 'string' && valor.length > 0 && valor.trim().length === 0
    ? { required: true }
    : null;
};

/**
 * Nombre de persona (mismas reglas que la API): solo letras, espacios, apóstrofes y guiones;
 * nombre y al menos un apellido; sin letras repetidas de forma anómala ("aaaa").
 */
export const nombrePersona: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = typeof control.value === 'string' ? control.value.trim() : '';
  if (!valor) {
    return null;
  }
  if (!/^\p{L}[\p{L}\p{M}' .-]*\p{L}\.?$/u.test(valor)) {
    return { caracteresNombre: true };
  }
  if (valor.split(/\s+/).filter((parte) => parte.length > 1).length < 2) {
    return { nombreIncompleto: true };
  }
  if (/(\p{L})\1{3,}/iu.test(valor)) {
    return { letrasRepetidas: true };
  }
  return null;
};

/** Carrera: debe empezar por una letra y solo admite letras, números y puntuación básica. */
export const textoCarrera: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = typeof control.value === 'string' ? control.value.trim() : '';
  return !valor || /^\p{L}[\p{L}\p{M}0-9 ,.()&/-]*$/u.test(valor)
    ? null
    : { caracteresCarrera: true };
};

/** Correo con dominio y extensión ("nombre@dominio.com"); el validador de Angular acepta "a@b". */
export const correoCompleto: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const valor = typeof control.value === 'string' ? control.value.trim() : '';
  return !valor || /^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$/.test(valor) ? null : { email: true };
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
