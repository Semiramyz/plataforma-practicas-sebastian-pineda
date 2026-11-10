import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PRACTICANTES } from '../../../testing/datos-prueba';
import { Panel } from './panel';

describe('Panel', () => {
  let fixture: ComponentFixture<Panel>;
  let elemento: HTMLElement;
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Panel],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Panel);
    elemento = fixture.nativeElement;
    backend = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    backend.expectOne('/api/practicantes').flush(PRACTICANTES);
    await fixture.whenStable();
  });

  afterEach(() => backend.verify());

  const filas = () => Array.from(elemento.querySelectorAll<HTMLTableRowElement>('tbody tr'));
  const nombres = () => filas().map((f) => f.querySelector('a.nombre')?.textContent?.trim());
  const indicador = (texto: string) =>
    Array.from(elemento.querySelectorAll<HTMLButtonElement>('.indicador')).find((b) =>
      b.textContent?.includes(texto),
    )!;

  it('lista los practicantes con su estado, puntaje y totales', () => {
    expect(nombres()).toEqual(['Carlos Díaz', 'Laura Ríos']);
    expect(filas()[0].textContent).toContain('—');
    expect(filas()[1].querySelector('.puntaje')?.textContent).toContain('80');
    expect(indicador('Registrados').textContent).toContain('2');
    expect(indicador('Por evaluar').textContent).toContain('1');
    expect(indicador('Viables').textContent).toContain('1');
    expect(elemento.textContent).toContain('80/100');
  });

  it('filtra por estado y por texto sin distinguir tildes', async () => {
    indicador('Viables').click();
    await fixture.whenStable();
    expect(filas()).toHaveLength(1);

    indicador('Registrados').click();
    const buscador = elemento.querySelector<HTMLInputElement>('input[type="search"]')!;
    buscador.value = 'diaz';
    buscador.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(nombres()).toEqual(['Carlos Díaz']);
  });

  it('ordena por puntaje', async () => {
    const orden = elemento.querySelector<HTMLSelectElement>('select')!;
    orden.value = 'puntaje';
    orden.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(nombres()).toEqual(['Laura Ríos', 'Carlos Díaz']);
  });

  it('enlaza el nombre de cada practicante con su evaluación (accesible con teclado)', () => {
    const enlace = filas()[0].querySelector<HTMLAnchorElement>('a.nombre')!;

    expect(enlace.getAttribute('href')).toBe('/analista/practicantes/2');
    expect(filas()[0].querySelector('th')?.getAttribute('scope')).toBe('row');
  });

  const accion = (fila: number) =>
    filas()[fila].querySelector('td.derecha a')?.textContent?.replace(/\s+/g, ' ').trim();

  it('muestra "Evaluar" para pendientes y "Ver" para evaluados, con el nombre para lectores de pantalla', () => {
    expect(accion(0)).toBe('Evaluar : Carlos Díaz');
    expect(accion(1)).toBe('Ver : Laura Ríos');
    expect(filas()[0].querySelector('.oculto-visualmente')?.textContent).toContain('Carlos Díaz');
  });

  it('marca "En evaluación" y ofrece continuar cuando hay un avance guardado', async () => {
    Array.from(elemento.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.includes('Actualizar'))!
      .click();
    backend.expectOne('/api/practicantes').flush([
      {
        ...PRACTICANTES[0],
        borrador: {
          calificacion: { ortografia: 3 },
          decision: null,
          motivosRechazo: [],
          observaciones: null,
          evaluador: 'Analista de selección',
          fechaActualizacionUtc: '2026-11-09T21:00:00Z',
        },
      },
      PRACTICANTES[1],
    ]);
    await fixture.whenStable();

    expect(filas()[0].textContent).toContain('En evaluación');
    expect(accion(0)).toContain('Continuar');
    expect(filas()[1].textContent).not.toContain('En evaluación');
  });
});
