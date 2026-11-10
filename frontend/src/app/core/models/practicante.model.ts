export type EstadoPracticante = 'Pendiente' | 'Viable' | 'NoViable';

/** Decisiones que puede tomar el analista. */
export type DecisionAnalista = Exclude<EstadoPracticante, 'Pendiente'>;

export type CriterioRubrica =
  'ortografia' | 'presentacion' | 'formacion' | 'experiencia' | 'habilidades';

export type Calificacion = Record<CriterioRubrica, number>;

export type MotivoRechazo =
  | 'ErroresOrtograficos'
  | 'InformacionIncompleta'
  | 'FormatoInadecuado'
  | 'PerfilNoAlineado'
  | 'ExperienciaInsuficiente'
  | 'DocumentoIlegible'
  | 'Otro';

export interface Evaluacion {
  calificacion: Calificacion;
  promedio: number;
  puntajeTotal: number;
  recomendacion: DecisionAnalista;
  motivosRechazo: MotivoRechazo[];
  observaciones: string | null;
  evaluador: string;
}

/** Evaluación en curso guardada automáticamente en la base de datos mientras se califica. */
export interface BorradorEvaluacion {
  calificacion: Partial<Record<CriterioRubrica, number | null>>;
  decision: DecisionAnalista | null;
  motivosRechazo: MotivoRechazo[];
  observaciones: string | null;
  evaluador: string;
  fechaActualizacionUtc: string;
}

/** Cuerpo para guardar el avance: cada criterio puede seguir sin calificar. */
export type NuevoBorrador = Partial<Record<CriterioRubrica, number>> & {
  decision: DecisionAnalista | null;
  motivosRechazo: MotivoRechazo[];
  observaciones: string | null;
};

/** Entrada del historial de auditoría: cada evaluación guardada, incluidas las corregidas. */
export interface RegistroEvaluacion {
  decision: DecisionAnalista;
  puntajeTotal: number;
  evaluador: string;
  fechaUtc: string;
}

export interface Practicante {
  id: number;
  nombreCompleto: string;
  correo: string;
  carrera: string;
  semestre: number;
  estado: EstadoPracticante;
  fechaRegistroUtc: string;
  fechaEvaluacionUtc: string | null;
  hojaDeVidaNombre: string;
  hojaDeVidaTamanoBytes: number;
  evaluacion: Evaluacion | null;
  /** Avance sin terminar de una evaluación; se recupera al volver a la pantalla. */
  borrador: BorradorEvaluacion | null;
  /** Historial de evaluaciones, de la más reciente a la más antigua. */
  historial: RegistroEvaluacion[];
}

export interface NuevaEvaluacion {
  decision: DecisionAnalista;
  calificacion: Calificacion;
  motivosRechazo: MotivoRechazo[];
  observaciones: string | null;
}

export interface NuevoPracticante {
  nombreCompleto: string;
  correo: string;
  carrera: string;
  semestre: number;
  hojaDeVida: File;
  autorizaTratamientoDatos: boolean;
}

export interface RegistroPracticante {
  id: number;
  nombreCompleto: string;
  fechaRegistroUtc: string;
}

export const ETIQUETAS_ESTADO: Record<EstadoPracticante, string> = {
  Pendiente: 'Pendiente',
  Viable: 'Viable',
  NoViable: 'No viable',
};

export interface DefinicionCriterio {
  clave: CriterioRubrica;
  nombre: string;
  descripcion: string;
  /** Guía concreta por nivel (1 a 5) para calificar de forma consistente. */
  niveles: [string, string, string, string, string];
  /** Motivo de rechazo que se sugiere cuando el criterio queda en 2 o menos. */
  motivoSugerido: MotivoRechazo;
}

/** Rúbrica de estándares de calidad de la hoja de vida. */
export const CRITERIOS: readonly DefinicionCriterio[] = [
  {
    clave: 'ortografia',
    nombre: 'Ortografía y redacción',
    descripcion: 'Tildes, puntuación, gramática y claridad del texto.',
    niveles: [
      'Más de 5 errores o texto confuso',
      '3 a 5 errores ortográficos',
      '1 o 2 errores menores',
      'Sin errores; redacción mejorable',
      'Sin errores y redacción clara',
    ],
    motivoSugerido: 'ErroresOrtograficos',
  },
  {
    clave: 'presentacion',
    nombre: 'Presentación y formato',
    descripcion: 'Orden, legibilidad, extensión (1 o 2 páginas) y datos de contacto visibles.',
    niveles: [
      'Desordenado o ilegible',
      'Difícil de leer o demasiado extenso',
      'Ordenado, con detalles por mejorar',
      'Claro y bien estructurado',
      'Impecable y profesional',
    ],
    motivoSugerido: 'FormatoInadecuado',
  },
  {
    clave: 'formacion',
    nombre: 'Formación académica',
    descripcion: 'Pertinencia de la carrera, avance, cursos y certificaciones.',
    niveles: [
      'No corresponde al perfil',
      'Relación débil con la práctica',
      'Pertinente, sin complementos',
      'Pertinente con cursos o certificaciones',
      'Destacada (logros, promedio, certificaciones)',
    ],
    motivoSugerido: 'PerfilNoAlineado',
  },
  {
    clave: 'experiencia',
    nombre: 'Experiencia y proyectos',
    descripcion: 'Proyectos académicos o personales, prácticas, voluntariados y logros medibles.',
    niveles: [
      'Sin proyectos ni experiencia',
      'Proyectos mencionados sin detalle',
      'Proyectos con su tecnología y rol',
      'Proyectos con resultados o repositorios',
      'Experiencia sólida con logros medibles',
    ],
    motivoSugerido: 'ExperienciaInsuficiente',
  },
  {
    clave: 'habilidades',
    nombre: 'Habilidades técnicas y blandas',
    descripcion: 'Herramientas y tecnologías relevantes, idiomas y competencias.',
    niveles: [
      'No describe habilidades',
      'Lista genérica sin relación con la práctica',
      'Habilidades relevantes básicas',
      'Buen dominio técnico e idioma',
      'Perfil técnico sobresaliente',
    ],
    motivoSugerido: 'InformacionIncompleta',
  },
];

export const NIVELES = ['Deficiente', 'Insuficiente', 'Aceptable', 'Bueno', 'Excelente'] as const;

export const MOTIVOS: readonly { clave: MotivoRechazo; etiqueta: string }[] = [
  { clave: 'ErroresOrtograficos', etiqueta: 'Errores ortográficos o de redacción' },
  { clave: 'InformacionIncompleta', etiqueta: 'Información incompleta o desactualizada' },
  { clave: 'FormatoInadecuado', etiqueta: 'Formato desordenado o difícil de leer' },
  { clave: 'PerfilNoAlineado', etiqueta: 'Perfil no alineado con la práctica' },
  { clave: 'ExperienciaInsuficiente', etiqueta: 'Sin proyectos ni experiencia demostrable' },
  { clave: 'DocumentoIlegible', etiqueta: 'Documento ilegible o dañado' },
  { clave: 'Otro', etiqueta: 'Otro (explícalo en las observaciones)' },
];

export const ETIQUETAS_MOTIVO = Object.fromEntries(
  MOTIVOS.map((m) => [m.clave, m.etiqueta]),
) as Record<MotivoRechazo, string>;

export const LONGITUD_MINIMA_JUSTIFICACION = 20;
export const LONGITUD_MAXIMA_OBSERVACIONES = 1000;

/** Puntaje de 0 a 100 (misma fórmula que la API). */
export function puntajeTotal(c: Calificacion): number {
  const suma = CRITERIOS.reduce((total, criterio) => total + c[criterio.clave], 0);
  return Math.round(((suma - 5) / 20) * 100);
}

export function promedio(c: Calificacion): number {
  const suma = CRITERIOS.reduce((total, criterio) => total + c[criterio.clave], 0);
  return Math.round((suma / 5) * 10) / 10;
}

/**
 * Recomendación según los estándares de calidad (misma regla que la API): "No viable" si la
 * ortografía está en 2 o menos, si algún criterio tiene 1 o si el promedio es menor que 3.
 */
export function recomendacion(c: Calificacion): DecisionAnalista {
  const valores = CRITERIOS.map((criterio) => c[criterio.clave]);
  return c.ortografia <= 2 || valores.includes(1) || promedio(c) < 3 ? 'NoViable' : 'Viable';
}
