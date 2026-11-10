# ADR 0004 · La rúbrica de evaluación y sus reglas viven en el dominio

**Estado:** aceptada

## Contexto

El reto pide un botón para marcar "Viable" o "No viable". Una decisión sin criterio no sirve en un proceso de selección: el analista debe poder rechazar, por ejemplo, por fallos ortográficos, y la decisión debe poder justificarse.

## Decisión

- Una **rúbrica de cinco criterios** (ortografía, presentación, formación, experiencia y habilidades), cada uno de 1 a 5, modelada como el *value object* `Calificacion`. Este calcula el puntaje (0-100), el promedio y una **recomendación**: "No viable" si la ortografía está en 2 o menos, si algún criterio tiene 1 o si el promedio es menor que 3.
- `ReglasEvaluacion` define cuándo una evaluación tiene sustento: rúbrica completa, motivos obligatorios para "No viable", explicación para el motivo "Otro" y justificación cuando la decisión contradice la recomendación.
- `Practicante.Evaluar(...)` aplica esas reglas y lanza `EvaluacionInvalidaException` si no se cumplen. El estado tiene `private set`: no hay otra forma de cambiarlo.
- La API reutiliza `ReglasEvaluacion` para devolver los errores campo por campo (400), y el frontend replica la fórmula **solo para mostrarla en vivo**.

## Consecuencias

- Ninguna capa puede guardar una decisión sin sustento, ni siquiera llamando a la API directamente.
- La fórmula de la recomendación existe en dos lugares (C# y TypeScript). Las pruebas de ambos lados cubren los mismos casos para detectar si se desalinean.
- Cambiar los criterios implica una migración de base de datos (las columnas `Evaluacion_*`).
