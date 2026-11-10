# ADR 0003 · JWT con lista de revocación y hash PBKDF2

**Estado:** aceptada

## Contexto

El reto pide una autenticación básica para que solo los analistas vean el panel (usuario y contraseña simples). La API también sirve a un cliente SPA y debe poder probarse con herramientas como `curl`.

## Decisión

- **JWT Bearer** firmado con HMAC-SHA256: emisor, audiencia, expiración y algoritmo validados, 60 minutos de vida.
- **Lista de revocación** (por `jti`, en memoria hasta la expiración del token): `POST /api/auth/logout` invalida el token de inmediato.
- **Contraseña como hash PBKDF2-SHA256** (210 000 iteraciones, sal de 16 bytes) en la configuración; se genera con `dotnet run -- --generar-hash`.
- **Límite de intentos** por IP en el login y respuesta indistinguible si el usuario no existe.
- **Política de autorización por defecto** que exige sesión: un endpoint nuevo es privado hasta que se marque `[AllowAnonymous]`.

## Alternativas consideradas

- **Cookie de sesión HttpOnly:** protege mejor el token frente a XSS, pero exige protección CSRF y complica el uso desde `curl`. Se mitigó el riesgo de XSS con una CSP estricta y con `sessionStorage`.
- **ASP.NET Core Identity:** es lo adecuado con varios usuarios y registro de cuentas; para un único analista de demo agregaría tablas y flujos que el reto no pide.

## Consecuencias

- La revocación en memoria se pierde al reiniciar la API (los tokens igual expiran en 60 minutos). Con varias instancias habría que pasarla a Redis.
- El frontend borra la sesión en tres casos: al cerrar sesión (y avisa a la API para revocar el token), al expirar el token y al recibir un 401.
