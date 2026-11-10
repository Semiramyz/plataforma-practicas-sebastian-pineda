# ADR 0005 · Historial de auditoría de evaluaciones de solo agregado

**Estado:** aceptada

## Contexto

Un analista puede corregir una evaluación. Si solo se guardara la última, se perdería el rastro de decisiones anteriores, que importa en un proceso de selección (trazabilidad y OWASP A09).

## Decisión

- Cada llamada exitosa a `Practicante.Evaluar` agrega un `RegistroEvaluacion` (decisión, puntaje, evaluador y fecha) a una colección que **solo crece**: el dominio no expone forma de editar ni borrar entradas.
- Se persiste en la tabla `HistorialEvaluaciones` como colección propia del practicante (*owned entities* de EF Core).
- La migración que crea la tabla también traslada al historial las evaluaciones que ya existían.
- Además, cada evaluación y cada evento de sesión se escribe en el log estructurado.

## Consecuencias

- El panel muestra el historial con la decisión vigente primero.
- Una evaluación rechazada por las reglas no deja rastro, porque no llegó a ser una decisión.
- El historial se borra en cascada solo si se borra el practicante, operación que la aplicación no ofrece.
