import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => backend.verify());

  function iniciarSesion(): void {
    auth.iniciarSesion('analista', 'clave').subscribe();
    backend.expectOne('/api/auth/login').flush({
      token: 'token-de-prueba',
      expiraUtc: new Date(Date.now() + 60_000).toISOString(),
      nombre: 'Analista',
    });
  }

  it('no envía Authorization sin sesión', () => {
    http.get('/api/practicantes').subscribe();

    expect(backend.expectOne('/api/practicantes').request.headers.has('Authorization')).toBe(false);
  });

  it('adjunta el token del analista a las llamadas de la API', () => {
    iniciarSesion();

    http.get('/api/practicantes').subscribe();

    expect(backend.expectOne('/api/practicantes').request.headers.get('Authorization')).toBe(
      'Bearer token-de-prueba',
    );
  });

  it('cierra la sesión y redirige al login cuando la API responde 401', () => {
    iniciarSesion();
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    http.get('/api/practicantes').subscribe({ error: () => undefined });
    backend.expectOne('/api/practicantes').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.estaAutenticado()).toBe(false);
    expect(navegar).toHaveBeenCalledWith(['/analista/login'], expect.anything());
  });

  it('al cerrar sesión avisa a la API para revocar el token y borra la sesión', () => {
    iniciarSesion();

    auth.cerrarSesion();

    const logout = backend.expectOne('/api/auth/logout');
    expect(logout.request.method).toBe('POST');
    expect(logout.request.headers.get('Authorization')).toBe('Bearer token-de-prueba');
    logout.flush(null, { status: 204, statusText: 'No Content' });
    expect(auth.estaAutenticado()).toBe(false);
    expect(sessionStorage.getItem('practicas.sesion')).toBeNull();
  });
});
