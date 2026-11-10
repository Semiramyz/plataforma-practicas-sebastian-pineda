import { Calificacion, promedio, puntajeTotal, recomendacion } from './practicante.model';

/**
 * Mismos casos que ReglasEvaluacionTests (C#): si la fórmula se desalinea entre el
 * frontend y la API, fallan las dos suites.
 */
const calificacion = ([o, p, f, e, h]: readonly number[]): Calificacion => ({
  ortografia: o,
  presentacion: p,
  formacion: f,
  experiencia: e,
  habilidades: h,
});

describe('rúbrica de calidad', () => {
  it.each([
    [[1, 1, 1, 1, 1], 0, 1.0],
    [[3, 3, 3, 3, 3], 50, 3.0],
    [[4, 4, 4, 4, 4], 75, 4.0],
    [[5, 5, 5, 5, 5], 100, 5.0],
    [[5, 4, 3, 4, 5], 80, 4.2],
  ] as const)('%j → puntaje %i y promedio %d', (valores, puntaje, prom) => {
    const c = calificacion(valores);
    expect(puntajeTotal(c)).toBe(puntaje);
    expect(promedio(c)).toBe(prom);
  });

  it.each([
    [[4, 4, 4, 4, 4], 'Viable'],
    [[3, 3, 3, 3, 3], 'Viable'],
    [[2, 5, 5, 5, 5], 'NoViable'], // ortografía deficiente
    [[5, 5, 1, 5, 5], 'NoViable'], // un criterio en 1
    [[3, 3, 2, 3, 2], 'NoViable'], // promedio 2,6
  ] as const)('%j → recomienda %s', (valores, esperada) => {
    expect(recomendacion(calificacion(valores))).toBe(esperada);
  });
});
