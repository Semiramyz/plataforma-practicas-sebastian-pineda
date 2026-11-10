import { FormControl } from '@angular/forms';
import {
  archivoPdf,
  correoCompleto,
  noVacio,
  nombrePersona,
  textoCarrera,
} from './formulario.validators';

function pdf(nombre = 'cv.pdf', bytes = 10, tipo = 'application/pdf'): File {
  return new File([new Uint8Array(bytes)], nombre, { type: tipo });
}

describe('noVacio', () => {
  it('marca como requerido un texto con solo espacios', () => {
    expect(noVacio(new FormControl('   '))).toEqual({ required: true });
  });

  it('acepta textos con contenido o vacíos (de eso se encarga required)', () => {
    expect(noVacio(new FormControl('Ana'))).toBeNull();
    expect(noVacio(new FormControl(''))).toBeNull();
  });
});

describe('archivoPdf', () => {
  const validar = archivoPdf(1024);

  it('acepta un PDF dentro del tamaño máximo', () => {
    expect(validar(new FormControl(pdf()))).toBeNull();
    expect(validar(new FormControl(pdf('CV.PDF')))).toBeNull();
  });

  it('no valida cuando no hay archivo', () => {
    expect(validar(new FormControl(null))).toBeNull();
  });

  it('rechaza archivos que no son PDF', () => {
    expect(validar(new FormControl(pdf('cv.docx', 10, 'application/msword')))).toEqual({
      tipoArchivo: true,
    });
    expect(validar(new FormControl(pdf('cv.pdf', 10, 'image/png')))).toEqual({ tipoArchivo: true });
  });

  it('rechaza archivos vacíos o que superan el tamaño máximo', () => {
    expect(validar(new FormControl(pdf('cv.pdf', 0)))).toEqual({ archivoVacio: true });
    expect(validar(new FormControl(pdf('cv.pdf', 2048)))).toEqual({
      tamanoArchivo: { maximo: 1024, actual: 2048 },
    });
  });
});

describe('nombrePersona', () => {
  it.each(['Ana Gómez', "María José O'Neil", 'Juan-Pablo de la Cruz', 'Ángela Núñez'])(
    'acepta "%s"',
    (nombre) => {
      expect(nombrePersona(new FormControl(nombre))).toBeNull();
    },
  );

  it.each([
    ['Ana 123', 'caracteresNombre'],
    ['<b>Ana</b> Gómez', 'caracteresNombre'],
    ['Ana', 'nombreIncompleto'],
    ['Aaaaana Gómez', 'letrasRepetidas'],
  ])('rechaza "%s" con %s', (nombre, error) => {
    expect(nombrePersona(new FormControl(nombre))).toEqual({ [error]: true });
  });
});

describe('correoCompleto y textoCarrera', () => {
  it('exige dominio con extensión en el correo', () => {
    expect(correoCompleto(new FormControl('ana@correo.com'))).toBeNull();
    expect(correoCompleto(new FormControl('ana@correo'))).toEqual({ email: true });
  });

  it('rechaza etiquetas HTML en la carrera', () => {
    expect(textoCarrera(new FormControl('Ingeniería de Sistemas (diurna)'))).toBeNull();
    expect(textoCarrera(new FormControl('<script>'))).toEqual({ caracteresCarrera: true });
  });
});
