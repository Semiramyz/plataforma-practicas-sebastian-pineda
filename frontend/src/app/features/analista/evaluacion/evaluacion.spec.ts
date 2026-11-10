import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Practicante } from '../../../core/models/practicante.model';
import { PRACTICANTES } from '../../../testing/datos-prueba';
import { RETARDO_AUTOGUARDADO_MS } from './autoguardado';
import { EvaluacionPagina } from './evaluacion';

describe('EvaluacionPagina', () => {
  let fixture: ComponentFixture<EvaluacionPagina>;
  let elemento: HTMLElement;
  let backend: HttpTestingController;

  beforeAll(() => {
    // jsdom no implementa URL.createObjectURL.
    URL.createObjectURL ??= () => 'blob:prueba';
    URL.revokeObjectURL ??= () => undefined;
  });

  /** Por defecto el guardado automático queda "dormido"; las pruebas del borrador usan 0 ms. */
  async function abrir(practicante: Practicante, retardoAutoguardado = 60_000): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [EvaluacionPagina],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RETARDO_AUTOGUARDADO_MS, useValue: retardoAutoguardado },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EvaluacionPagina);
    fixture.componentRef.setInput('id', String(practicante.id));
    elemento = fixture.nativeElement;
    backend = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();

    backend.expectOne(`/api/practicantes/${practicante.id}`).flush(practicante);
    await fixture.whenStable();
    backend
      .expectOne(`/api/practicantes/${practicante.id}/hoja-de-vida`)
      .flush(new Blob(['%PDF-1.4'], { type: 'application/pdf' }));
    await fixture.whenStable();
  }

  afterEach(() => backend.verify());

  const pendiente = () => PRACTICANTES[0];
  const botonGuardar = () =>
    Array.from(elemento.querySelectorAll<HTMLButtonElement>('button')).find((b) =>
      b.textContent?.includes('Guardar evaluación'),
    )!;
  const pendientes = () => elemento.querySelector('.pendientes')?.textContent ?? '';
  const texto = (selector: string) =>
    elemento.querySelector(selector)?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
  /** Deja correr el temporizador del guardado automático (0 ms en estas pruebas). */
  const esperarAutoguardado = async () => {
    await new Promise((r) => setTimeout(r));
    await fixture.whenStable();
  };

  /** Intenta guardar sin completar: no debe enviar nada y debe explicar lo que falta. */
  async function intentarGuardarIncompleto(): Promise<void> {
    botonGuardar().click();
    await fixture.whenStable();
    backend.expectNone('/api/practicantes/2/evaluacion');
    expect(pendientes()).toContain('Todavía no se puede guardar');
  }
  const decision = (valor: 'Viable' | 'NoViable') =>
    elemento.querySelector<HTMLInputElement>(`input[name="decision"][value="${valor}"]`)!;

  async function calificar(valores: Record<string, number>): Promise<void> {
    for (const [criterio, valor] of Object.entries(valores)) {
      const radio = elemento.querySelector<HTMLInputElement>(
        `input[name="${criterio}"][value="${valor}"]`,
      )!;
      radio.checked = true;
      radio.dispatchEvent(new Event('change'));
    }
    await fixture.whenStable();
  }

  async function elegir(valor: 'Viable' | 'NoViable'): Promise<void> {
    decision(valor).checked = true;
    decision(valor).dispatchEvent(new Event('change'));
    await fixture.whenStable();
  }

  const borrador = (calificacion: Record<string, number>, observaciones: string | null = null) => ({
    calificacion,
    decision: null,
    motivosRechazo: [],
    observaciones,
    evaluador: 'Analista de selección',
    fechaActualizacionUtc: '2026-11-09T21:00:00Z',
  });

  const todos = (valor: number) => ({
    ortografia: valor,
    presentacion: valor,
    formacion: valor,
    experiencia: valor,
    habilidades: valor,
  });

  it('no permite decidir ni guardar hasta calificar los 5 criterios', async () => {
    await abrir(pendiente());

    expect(decision('Viable').disabled).toBe(true);
    expect(pendientes()).toContain('los 5 criterios restantes');
    await intentarGuardarIncompleto();

    await calificar({ ortografia: 4, presentacion: 4 });
    expect(pendientes()).toContain('los 3 criterios restantes');
    expect(decision('Viable').disabled).toBe(true);
  });

  it('calcula el puntaje y la recomendación en vivo', async () => {
    await abrir(pendiente());

    await calificar(todos(4));

    expect(elemento.querySelector('.resultado--vivo')?.textContent).toContain('75');
    expect(elemento.querySelector('.resultado--vivo')?.textContent).toContain('Viable');
    expect(decision('Viable').disabled).toBe(false);
  });

  it('recomienda "No viable" por ortografía deficiente y sugiere el motivo', async () => {
    await abrir(pendiente());

    await calificar({ ...todos(4), ortografia: 1 });
    expect(elemento.querySelector('.resultado--vivo')?.textContent).toContain('ortografía');

    await elegir('NoViable');

    const marcados = Array.from(
      elemento.querySelectorAll<HTMLInputElement>('.motivo input:checked'),
    ).map((i) => i.parentElement?.textContent);
    expect(marcados.join(' ')).toContain('Errores ortográficos');
    expect(elemento.querySelector('.pendientes')).toBeNull();
  });

  it('exige justificación si la decisión contradice la rúbrica', async () => {
    await abrir(pendiente());
    await calificar({ ...todos(4), ortografia: 2 });

    await elegir('Viable');

    expect(pendientes()).toContain('Justifica');
    await intentarGuardarIncompleto();

    const observaciones = elemento.querySelector<HTMLTextAreaElement>('#observaciones')!;
    observaciones.value = 'Errores menores; su portafolio de proyectos es sobresaliente.';
    observaciones.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(elemento.querySelector('.pendientes')).toBeNull();
  });

  it('exige al menos un motivo para "No viable"', async () => {
    await abrir(pendiente());
    await calificar(todos(4));
    await elegir('NoViable'); // sin criterios bajos no hay motivos sugeridos

    expect(pendientes()).toContain('al menos un motivo');
    await intentarGuardarIncompleto();
  });

  it('guarda la evaluación completa y muestra el resumen', async () => {
    await abrir(pendiente());
    await calificar({ ...todos(3), ortografia: 1 });
    await elegir('NoViable');

    botonGuardar().click();

    const solicitud = backend.expectOne('/api/practicantes/2/evaluacion');
    expect(solicitud.request.method).toBe('PUT');
    expect(solicitud.request.body).toEqual({
      decision: 'NoViable',
      calificacion: { ...todos(3), ortografia: 1 },
      motivosRechazo: ['ErroresOrtograficos'],
      observaciones: null,
    });
    solicitud.flush({
      ...pendiente(),
      estado: 'NoViable',
      fechaEvaluacionUtc: '2026-11-09T10:00:00Z',
      evaluacion: {
        calificacion: { ...todos(3), ortografia: 1 },
        promedio: 2.6,
        puntajeTotal: 40,
        recomendacion: 'NoViable',
        motivosRechazo: ['ErroresOrtograficos'],
        observaciones: null,
        evaluador: 'Analista de selección',
      },
    });
    await fixture.whenStable();
    backend.expectOne('/api/practicantes').flush(PRACTICANTES);
    await fixture.whenStable();

    expect(elemento.querySelector('[role="status"]')?.textContent).toContain('Evaluación guardada');
    expect(elemento.querySelector('.resumen')?.textContent).toContain('40');
    expect(elemento.querySelector('.chips')?.textContent).toContain('Errores ortográficos');
    expect(fixture.componentInstance.tieneCambiosSinGuardar()).toBe(false);
  });

  it('guarda el avance en la base de datos mientras se califica', async () => {
    await abrir(pendiente(), 0);
    expect(texto('.autoguardado')).toContain('se guarda automáticamente');

    await calificar({ ortografia: 2 });
    await esperarAutoguardado();

    const primero = backend.expectOne('/api/practicantes/2/evaluacion/borrador');
    expect(primero.request.method).toBe('PUT');
    expect(primero.request.body).toEqual({
      ortografia: 2,
      decision: null,
      motivosRechazo: [],
      observaciones: null,
    });
    expect(fixture.componentInstance.tieneCambiosSinGuardar()).toBe(false);
    primero.flush(borrador({ ortografia: 2 }));
    await fixture.whenStable();
    expect(texto('.autoguardado')).toContain('Avance guardado automáticamente');

    // El comentario también se guarda.
    const observaciones = elemento.querySelector<HTMLTextAreaElement>('#observaciones')!;
    observaciones.value = 'Tildes faltantes en el perfil.';
    observaciones.dispatchEvent(new Event('input'));
    await esperarAutoguardado();
    const segundo = backend.expectOne('/api/practicantes/2/evaluacion/borrador');
    expect(segundo.request.body.observaciones).toBe('Tildes faltantes en el perfil.');
    segundo.flush(borrador({ ortografia: 2 }, 'Tildes faltantes en el perfil.'));
  });

  it('avisa si el avance no se pudo guardar y permite reintentar', async () => {
    await abrir(pendiente(), 0);
    await calificar({ ortografia: 3 });
    await esperarAutoguardado();

    backend
      .expectOne('/api/practicantes/2/evaluacion/borrador')
      .flush(null, { status: 500, statusText: 'Error' });
    await fixture.whenStable();

    expect(texto('.autoguardado')).toContain('No se pudo guardar el avance');
    expect(fixture.componentInstance.tieneCambiosSinGuardar()).toBe(true);
    // tieneCambiosSinGuardar() reintenta el envío al intentar salir.
    backend.expectOne('/api/practicantes/2/evaluacion/borrador').flush(borrador({ ortografia: 3 }));
  });

  it('recupera las secciones calificadas y el comentario al volver a la evaluación', async () => {
    await abrir({
      ...pendiente(),
      borrador: {
        calificacion: {
          ortografia: 2,
          presentacion: 4,
          formacion: null,
          experiencia: null,
          habilidades: null,
        },
        decision: null,
        motivosRechazo: [],
        observaciones: 'Revisar la sección de proyectos.',
        evaluador: 'Analista de selección',
        fechaActualizacionUtc: '2026-11-09T21:00:00Z',
      },
    });

    const marcado = (criterio: string, valor: number) =>
      elemento.querySelector<HTMLInputElement>(`input[name="${criterio}"][value="${valor}"]`)!
        .checked;
    expect(marcado('ortografia', 2)).toBe(true);
    expect(marcado('presentacion', 4)).toBe(true);
    expect(texto('.paso__ayuda')).toContain('2 de 5 criterios');
    expect(elemento.querySelector<HTMLTextAreaElement>('#observaciones')!.value).toBe(
      'Revisar la sección de proyectos.',
    );
    expect(texto('.alerta--exito')).toContain('Se recuperó la evaluación que dejaste en curso');
  });

  it('descartar el avance lo borra de la base de datos', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    await abrir({
      ...pendiente(),
      borrador: borrador({ ortografia: 1 }),
    });

    Array.from(elemento.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.includes('Descartar avance'))!
      .click();
    await fixture.whenStable();

    expect(backend.expectOne('/api/practicantes/2/evaluacion/borrador').request.method).toBe(
      'DELETE',
    );
    expect(
      elemento.querySelector<HTMLInputElement>('input[name="ortografia"][value="1"]')!.checked,
    ).toBe(false);
  });

  it('espera a que termine el guardado automático antes de guardar la evaluación', async () => {
    await abrir(pendiente(), 0);
    await calificar(todos(4));
    await elegir('Viable');
    await esperarAutoguardado();
    const enCamino = backend.match('/api/practicantes/2/evaluacion/borrador');
    expect(enCamino.length).toBeGreaterThan(0);

    botonGuardar().click();
    await fixture.whenStable();
    backend.expectNone('/api/practicantes/2/evaluacion');

    enCamino.forEach((r) => r.flush(borrador(todos(4))));
    await fixture.whenStable();

    const definitiva = backend.expectOne('/api/practicantes/2/evaluacion');
    expect(definitiva.request.body.decision).toBe('Viable');
    definitiva.flush({ ...PRACTICANTES[1], id: 2 });
    await fixture.whenStable();
    backend.expectOne('/api/practicantes').flush(PRACTICANTES);
  });

  it('muestra el resumen de una evaluación existente y permite editarla', async () => {
    await abrir(PRACTICANTES[1]);

    expect(elemento.querySelector('.resumen')?.textContent).toContain('80');
    expect(elemento.querySelector('.resumen')?.textContent).toContain('Analista de selección');

    // Historial de auditoría: la decisión vigente primero y la corregida debajo.
    const historial = Array.from(elemento.querySelectorAll('.historial li')).map((li) =>
      li.textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(historial).toHaveLength(2);
    expect(historial[0]).toContain('Viable');
    expect(historial[0]).toContain('vigente');
    expect(historial[1]).toContain('No viable');
    expect(historial[1]).toContain('45/100');

    // Cada sección de la rúbrica se muestra con su nivel y la guía de ese nivel.
    const secciones = Array.from(elemento.querySelectorAll('.seccion-calificada')).map((li) =>
      li.textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(secciones).toHaveLength(5);
    expect(secciones[0]).toContain('Ortografía y redacción');
    expect(secciones[0]).toContain('5/5 · Excelente');
    expect(secciones[0]).toContain('Sin errores y redacción clara');
    expect(secciones[1]).toContain('4/5 · Bueno');
    expect(texto('.resumen')).toContain('Sin comentarios');

    // Las barras de la rúbrica se exponen como medidores accesibles.
    const ortografia = elemento.querySelector('[role="meter"]')!;
    expect(ortografia.getAttribute('aria-valuenow')).toBe('5');
    expect(ortografia.getAttribute('aria-label')).toBe('Ortografía y redacción: 5 de 5');

    Array.from(elemento.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.includes('Editar evaluación'))!
      .click();
    await fixture.whenStable();

    expect(
      elemento.querySelector<HTMLInputElement>('input[name="ortografia"][value="5"]')!.checked,
    ).toBe(true);
    expect(decision('Viable').checked).toBe(true);
  });
});
