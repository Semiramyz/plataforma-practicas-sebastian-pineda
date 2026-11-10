# ADR 0006 · Borrador de evaluación con guardado automático en la base de datos

**Estado:** aceptada

## Contexto

Evaluar una hoja de vida con la rúbrica toma varios minutos: leer el PDF, calificar cinco secciones, decidir y comentar. Antes, nada se guardaba hasta pulsar "Guardar evaluación" al final, y ese botón quedaba deshabilitado en silencio mientras faltara algo. Si el analista salía de la página o algo quedaba incompleto, se perdía lo calificado y parecía que "los puntajes no se guardan".

## Decisión

- El dominio tiene un **`BorradorEvaluacion`** por practicante (columnas `Borrador_*`), con criterios opcionales (`CalificacionParcial`), decisión tentativa, motivos, comentario, evaluador y fecha. `Practicante.GuardarBorrador(...)` solo valida formato (rangos y longitudes); las reglas completas siguen aplicándose únicamente a la decisión definitiva.
- El frontend guarda el borrador **automáticamente** con `PUT /api/practicantes/{id}/evaluacion/borrador`, 800 ms después del último cambio (agrupa las pulsaciones de teclado), y muestra el estado: guardando, guardado con la hora o error con reintento.
- Al volver a la pantalla, si hay borrador se abre en modo edición con las secciones y el comentario cargados.
- `Practicante.Evaluar(...)` **descarta el borrador** al guardar la evaluación definitiva. Si hay un guardado automático en camino, el frontend espera a que termine antes de enviar la evaluación, para que el borrador no llegue después y "reviva".
- Al salir de la página, lo pendiente se envía de inmediato; solo se pide confirmación si el último guardado automático falló.

## Alternativas consideradas

- **Guardar el borrador en `localStorage`:** no sobrevive a cambiar de equipo o de navegador, no lo ve el panel ("En evaluación") y deja datos personales en el navegador.
- **Guardar la evaluación definitiva con campos opcionales:** rompería la regla de no decidir sin sustento (ADR 0004).

## Consecuencias

- El trabajo del analista no se pierde, y el panel muestra qué evaluaciones están en curso.
- Hay más solicitudes a la API mientras se califica (una cada vez que el analista se detiene); el retardo las agrupa.
- Un borrador por practicante: si dos analistas evalúan al mismo practicante a la vez, gana el último guardado. Con varios analistas habría que guardar un borrador por analista.
