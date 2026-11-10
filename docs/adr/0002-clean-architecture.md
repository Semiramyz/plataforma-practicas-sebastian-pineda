# ADR 0002 · Clean Architecture en cuatro proyectos

**Estado:** aceptada. Reemplaza la primera versión, que era un solo proyecto con carpetas.

## Contexto

La primera versión tenía todo el backend en un proyecto. Era suficiente para el reto, pero las reglas de negocio (la rúbrica, los motivos de rechazo) dependían de detalles de infraestructura (EF Core, `IFormFile`) y no se podían probar sin levantar la API.

## Decisión

Separar el backend en capas con la regla de dependencias de Clean Architecture: **las dependencias apuntan hacia adentro**.

```
Practicas.Api ──────────┐
   │                    ▼
   │          Practicas.Infrastructure
   │                    │
   ▼                    ▼
Practicas.Application ◄─┘
   │
   ▼
Practicas.Domain  (sin dependencias)
```

| Proyecto | Responsabilidad | Puede depender de |
| --- | --- | --- |
| `Practicas.Domain` | Entidades, rúbrica (`Calificacion`), reglas de evaluación, historial | Nada |
| `Practicas.Application` | Casos de uso (`PracticanteService`), puertos (`IPracticanteRepositorio`, `IAlmacenamientoArchivos`, `IAutenticacionService`), validadores de negocio | Domain |
| `Practicas.Infrastructure` | Adaptadores: EF Core + SQLite, archivos en disco, JWT, PBKDF2, revocación de tokens | Application, Domain |
| `Practicas.Api` | HTTP: controladores, contratos, validación de modelos, cabeceras de seguridad, límites, composición | Todas (raíz de composición) |

La regla no depende de la disciplina: `Practicas.Architecture.Tests` revisa las referencias de cada ensamblado y falla si una capa interna referencia a una externa o a un framework.

## Consecuencias

- Los casos de uso se prueban con dobles en memoria, sin base de datos (`PracticanteServiceTests`).
- La API traduce las solicitudes HTTP (`IFormFile`, DataAnnotations) a comandos de la aplicación (`RegistrarPracticanteComando`); la aplicación no conoce HTTP.
- Hay más proyectos y algo más de código de mapeo. Para un sistema de este tamaño es un costo bajo a cambio de reglas de negocio aisladas y verificables.
- No se usó MediatR ni CQRS: con cinco casos de uso un servicio de aplicación es más simple y suficiente.
