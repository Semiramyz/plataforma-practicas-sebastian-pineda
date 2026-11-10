import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Registro } from './registro';

describe('Registro', () => {
  let fixture: ComponentFixture<Registro>;
  let elemento: HTMLElement;
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Registro],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Registro);
    elemento = fixture.nativeElement;
    backend = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => backend.verify());

  function escribir(id: string, valor: string): void {
    const input = elemento.querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)!;
    input.value = valor;
    input.dispatchEvent(new Event(input instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  function adjuntar(archivo: File): void {
    const input = elemento.querySelector<HTMLInputElement>('#hojaDeVida')!;
    Object.defineProperty(input, 'files', { value: [archivo], configurable: true });
    input.dispatchEvent(new Event('change'));
  }

  async function enviar(): Promise<void> {
    elemento.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();
  }

  function error(campo: string): string {
    return elemento.querySelector(`#error-${campo}`)?.textContent?.trim() ?? '';
  }

  async function llenarFormularioValido(): Promise<void> {
    escribir('nombreCompleto', 'Ana María Gómez');
    escribir('correo', 'ana@correo.com');
    escribir('carrera', 'Ingeniería de Sistemas');
    const semestre = elemento.querySelector<HTMLSelectElement>('#semestre')!;
    semestre.selectedIndex = 8; // 8.º semestre (el índice 0 es "Selecciona")
    semestre.dispatchEvent(new Event('change'));
    adjuntar(new File(['%PDF-1.4'], 'cv.pdf', { type: 'application/pdf' }));
    autorizar();
    await fixture.whenStable();
  }

  function autorizar(): void {
    const casilla = elemento.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
    casilla.checked = true;
    casilla.dispatchEvent(new Event('change'));
  }

  it('muestra los errores de los campos obligatorios y no envía nada', async () => {
    await enviar();

    expect(error('nombreCompleto')).toBe('Escribe tu nombre completo.');
    expect(error('correo')).toBe('Escribe tu correo electrónico.');
    expect(error('carrera')).toBe('Escribe tu carrera universitaria.');
    expect(error('semestre')).toBe('Selecciona tu semestre actual.');
    expect(error('hojaDeVida')).toBe('Adjunta tu hoja de vida en PDF.');
    expect(error('autorizaTratamientoDatos')).toContain('Debes autorizar');
    backend.expectNone('/api/practicantes');
  });

  it.each([
    ['Ana 123', 'solo puede contener letras'],
    ['<script>', 'solo puede contener letras'],
    ['Ana', 'al menos un apellido'],
    ['Annnna Gómez', 'letras repetidas'],
  ])('rechaza el nombre "%s"', async (nombre, mensaje) => {
    escribir('nombreCompleto', nombre);
    await enviar();

    expect(error('nombreCompleto')).toContain(mensaje);
  });

  it('no envía si falta la autorización de datos aunque lo demás esté bien', async () => {
    await llenarFormularioValido();
    const casilla = elemento.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
    casilla.checked = false;
    casilla.dispatchEvent(new Event('change'));
    await enviar();

    backend.expectNone('/api/practicantes');
    expect(error('autorizaTratamientoDatos')).toContain('Debes autorizar');
  });

  it('envía el nombre en formato de nombre propio si llegó en mayúsculas', async () => {
    await llenarFormularioValido();
    escribir('nombreCompleto', 'ANA MARÍA DE LA ROSA');
    await enviar();

    const cuerpo = backend.expectOne('/api/practicantes').request.body as FormData;
    expect(cuerpo.get('nombreCompleto')).toBe('Ana María de la Rosa');
    expect(cuerpo.get('autorizaTratamientoDatos')).toBe('true');
  });

  it('rechaza un nombre con solo espacios y un correo inválido', async () => {
    escribir('nombreCompleto', '    ');
    escribir('correo', 'no-es-un-correo');
    await enviar();

    expect(error('nombreCompleto')).toBe('Escribe tu nombre completo.');
    expect(error('correo')).toContain('correo válido');
  });

  it('rechaza una hoja de vida que no es PDF', async () => {
    adjuntar(new File(['hola'], 'cv.docx', { type: 'application/msword' }));
    await fixture.whenStable();

    expect(error('hojaDeVida')).toBe('La hoja de vida debe ser un archivo PDF.');
  });

  it('envía el formulario como multipart y muestra la confirmación', async () => {
    await llenarFormularioValido();
    await enviar();

    const solicitud = backend.expectOne('/api/practicantes');
    const cuerpo = solicitud.request.body as FormData;
    expect(solicitud.request.method).toBe('POST');
    expect(cuerpo.get('nombreCompleto')).toBe('Ana María Gómez');
    expect(cuerpo.get('semestre')).toBe('8');
    expect((cuerpo.get('hojaDeVida') as File).name).toBe('cv.pdf');

    solicitud.flush({
      id: 7,
      nombreCompleto: 'Ana María Gómez',
      fechaRegistroUtc: '2026-11-08T15:00:00Z',
    });
    await fixture.whenStable();

    expect(elemento.textContent).toContain('¡Registro enviado!');
    expect(elemento.textContent).toContain('#7');
  });

  it('muestra el mensaje del servidor cuando el correo ya existe', async () => {
    await llenarFormularioValido();
    await enviar();

    backend.expectOne('/api/practicantes').flush(
      {
        title: 'Correo ya registrado',
        detail: 'Ya existe un practicante registrado con el correo ana@correo.com.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();

    expect(elemento.querySelector('[role="alert"]')?.textContent).toContain(
      'Ya existe un practicante',
    );
  });
});
