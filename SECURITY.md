# Seguridad

Este documento resume cómo la plataforma aborda los riesgos del **OWASP Top 10 (2021)**, qué verificaciones automáticas lo respaldan y qué límites tiene esta versión.

El principio general: **el frontend solo guía al usuario; la API valida y autoriza todo por su cuenta.** Saltarse la interfaz (por ejemplo, llamando a la API con `curl`) no permite hacer nada que la interfaz no permita.

## Correspondencia con el OWASP Top 10

| Riesgo OWASP | Medidas en este proyecto | Dónde se verifica |
| --- | --- | --- |
| **A01 · Control de acceso roto** | Política de autorización por defecto: todo endpoint exige sesión salvo los marcados `[AllowAnonymous]` (solo `login` y el registro público). Rol `Analista` en cada acción del panel. Guard de Angular en las rutas del analista. Las evaluaciones no pueden saltarse la rúbrica (reglas en el dominio). | `ReglasDeArquitecturaTests` (ningún endpoint queda público por olvido), `PanelAnalistaTests` (401 sin token o con token falsificado) |
| **A02 · Fallas criptográficas** | Contraseña guardada como hash **PBKDF2-SHA256** (210 000 iteraciones, sal aleatoria) y comparación en tiempo constante. JWT firmado con HMAC-SHA256, algoritmo fijado al validar. Clave de firma de al menos 32 caracteres. Fuera de desarrollo: **HTTPS obligatorio y HSTS**. | `SeguridadTests`, validación de opciones al iniciar |
| **A03 · Inyección** | EF Core con consultas parametrizadas (la búsqueda nunca concatena SQL). Angular escapa toda interpolación. Validación de nombre y carrera que rechaza `<`, `>` y símbolos. **Content-Security-Policy** estricta (`script-src 'self'`). | `RegistroPracticanteTests` (nombres con etiquetas HTML), prueba de punta a punta sin violaciones de CSP |
| **A04 · Diseño inseguro** | Reglas de negocio en el dominio (`Practicante.Evaluar`): no se puede decidir sin rúbrica completa, "No viable" exige motivos y contradecir la recomendación exige justificación. Límite de 10 registros por IP por hora y de 5 intentos de login cada 5 minutos. | `ReglasEvaluacionTests`, `PracticanteTests`, `SeguridadTests` (429) |
| **A05 · Configuración incorrecta** | Cabeceras: `Content-Security-Policy`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`. `Cache-Control: no-store` en la API. Errores como `ProblemDetails` sin trazas internas. CORS limitado a orígenes, métodos y cabeceras concretos. OpenAPI solo en desarrollo. | `SeguridadTests` (cabeceras) |
| **A06 · Componentes vulnerables** | Auditoría de NuGet en cada restauración (`NuGetAudit`, incluidas transitivas) y `npm audit` en CI. Versiones centralizadas (`Directory.Packages.props`). Dependabot semanal. Se forzó `minimatch` ≥ 11.0.1 en las herramientas de lint (GHSA-g3hv-75wg-m249). | Pipeline de CI: `dotnet list package --vulnerable` y `npm audit` |
| **A07 · Fallas de identificación y autenticación** | Bloqueo por fuerza bruta (429 con `Retry-After`). Mismo mensaje y mismo tiempo de respuesta si el usuario no existe o la contraseña es incorrecta. Sesión de 60 minutos que se cierra sola al expirar. **El cierre de sesión revoca el token en el servidor.** Token en `sessionStorage`, que se borra al cerrar la pestaña. Longitud máxima de contraseña para evitar abuso del hash. | `SeguridadTests`, `PanelAnalistaTests.Logout_InvalidaElToken...`, `auth.interceptor.spec.ts` |
| **A08 · Fallas de integridad de datos y software** | Se valida la **firma real `%PDF-`** del archivo (no solo la extensión) y se **rechazan PDF con JavaScript, acciones de lanzamiento, archivos incrustados, RichMedia o XFA**. Los archivos se guardan con nombre aleatorio (GUID) fuera de cualquier carpeta pública; el nombre del usuario nunca forma una ruta (*path traversal*). | `ValidadorPdfTests`, `RegistroPracticanteTests` |
| **A09 · Fallas de registro y monitoreo** | **Historial de auditoría** persistente: cada evaluación guarda quién decidió, cuándo, qué decidió y con qué puntaje, incluidas las corregidas. Eventos de seguridad en el log estructurado (login exitoso o fallido con IP, cierre de sesión, evaluaciones). Nunca se registran contraseñas ni tokens. | `PracticanteTests.Historial_*`, `PanelAnalistaTests.Evaluar_PermiteCorregirYConservaElHistorial...` |
| **A10 · SSRF** | La API no hace solicitudes a URLs proporcionadas por el usuario. | — |

## Datos personales

- El registro exige la **autorización de tratamiento de datos (Ley 1581 de 2012)** y guarda su fecha.
- Las hojas de vida solo las ven analistas autenticados, y las respuestas de la API no se guardan en la caché del navegador.

## Verificaciones automáticas

| Verificación | Herramienta |
| --- | --- |
| Reglas de dependencias entre capas y autorización explícita de cada endpoint | `Practicas.Architecture.Tests` |
| Seguridad de la API (401, 429, cabeceras, revocación, PDF malicioso) | `Practicas.Api.Tests` |
| Analizadores de .NET (`latest-recommended`, advertencias como errores) | `dotnet build` |
| Lint de TypeScript y accesibilidad de plantillas | `npm run lint` (angular-eslint) |
| Vulnerabilidades en dependencias | `NuGetAudit`, `dotnet list package --vulnerable`, `npm audit`, Dependabot |

## Límites conocidos de esta versión

- La lista de tokens revocados vive en memoria: al reiniciar la API se pierde, aunque los tokens expiran en 60 minutos. Con varias instancias se usaría Redis.
- La búsqueda de contenido activo en el PDF no descomprime los flujos internos; en producción se sumaría un antivirus (por ejemplo, ClamAV) para los archivos subidos.
- Los límites por IP usan la IP de la conexión. Detrás de un proxy inverso habría que configurar `ForwardedHeaders` con la lista de proxies de confianza.
- Para la demo local, las credenciales de desarrollo están en `appsettings.Development.json`. En un despliegue real irían en un gestor de secretos (Azure Key Vault, variables de entorno protegidas), con varios usuarios en base de datos (ASP.NET Core Identity).

## Reportar una vulnerabilidad

Si encuentras un problema de seguridad, no abras un *issue* público: escríbeme directamente por GitHub ([@Semiramyz](https://github.com/Semiramyz)) con los pasos para reproducirlo.
