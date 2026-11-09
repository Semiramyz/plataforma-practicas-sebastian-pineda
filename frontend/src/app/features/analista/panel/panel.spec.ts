import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Practicante } from '../../../core/models/practicante.model';
import { Panel } from './panel';

const PRACTICANTES: Practicante[] = [
  {
    id: 2,
    nombreCompleto: 'Carlos Díaz',
    correo: 'carlos@correo.com',
    carrera: 'Ingeniería Industrial',
    semestre: 6,
    estado: 'Pendiente',
    fechaRegistroUtc: '2026-11-08T15:00:00Z',
    fechaEvaluacionUtc: null,
    hojaDeVidaNombre: 'cv-carlos.pdf',
    hojaDeVidaTamanoBytes: 120_000,
  },
  {
    id: 1,
    nombreCompleto: 'Laura Ríos',
    correo: 'laura@correo.com',
    carrera: 'Ingeniería de Sistemas',
    semestre: 8,
    estado: 'Viable',
    fechaRegistroUtc: '2026-11-07T15:00:00Z',
    fechaEvaluacionUtc: '2026-11-08T10:00:00Z',
    hojaDeVidaNombre: 'cv-laura.pdf',
    hojaDeVidaTamanoBytes: 80_000,
  },
];

describe('Panel', () => {
  let fixture: ComponentFixture<Panel>;
  let elemento: HTMLElement;
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Panel],
      providers: [provideHttpClient(), provideHttpClientTesting()],
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
  const indicador = (texto: string) =>
    Array.from(elemento.querySelectorAll<HTMLButtonElement>('.indicador')).find((b) =>
      b.textContent?.includes(texto),
    )!;

  it('lista los practicantes con su estado y muestra los totales', () => {
    expect(filas().map((f) => f.querySelector('strong')?.textContent)).toEqual([
      'Carlos Díaz',
      'Laura Ríos',
    ]);
    expect(indicador('Registrados').textContent).toContain('2');
    expect(indicador('Pendientes').textContent).toContain('1');
    expect(indicador('Viables').textContent).toContain('1');
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

    expect(filas().map((f) => f.querySelector('strong')?.textContent)).toEqual(['Carlos Díaz']);
  });

  it('muestra el detalle y marca al practicante como viable', async () => {
    filas()[0].click();
    await fixture.whenStable();

    const detalle = elemento.querySelector('aside')!;
    expect(detalle.textContent).toContain('carlos@correo.com');
    expect(detalle.textContent).toContain('cv-carlos.pdf');

    const botonViable = Array.from(detalle.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === 'Viable',
    )!;
    botonViable.click();

    const solicitud = backend.expectOne('/api/practicantes/2/estado');
    expect(solicitud.request.method).toBe('PATCH');
    expect(solicitud.request.body).toEqual({ estado: 'Viable' });
    solicitud.flush({
      ...PRACTICANTES[0],
      estado: 'Viable',
      fechaEvaluacionUtc: '2026-11-08T16:00:00Z',
    });
    await fixture.whenStable();

    expect(filas()[0].querySelector('.insignia')?.textContent).toBe('Viable');
    expect(indicador('Viables').textContent).toContain('2');
    expect(detalle.querySelector('[role="status"]')?.textContent).toContain(
      'marcado como "Viable"',
    );
  });

  it('marca al practicante como no viable', async () => {
    filas()[1].click();
    await fixture.whenStable();

    Array.from(elemento.querySelectorAll<HTMLButtonElement>('aside button'))
      .find((b) => b.textContent?.trim() === 'No viable')!
      .click();

    const solicitud = backend.expectOne('/api/practicantes/1/estado');
    expect(solicitud.request.body).toEqual({ estado: 'NoViable' });
    solicitud.flush({ ...PRACTICANTES[1], estado: 'NoViable' });
    await fixture.whenStable();

    expect(indicador('No viables').textContent).toContain('1');
  });
});
