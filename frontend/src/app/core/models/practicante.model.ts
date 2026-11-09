export type EstadoPracticante = 'Pendiente' | 'Viable' | 'NoViable';

/** Decisiones que puede tomar el analista. */
export type DecisionAnalista = Exclude<EstadoPracticante, 'Pendiente'>;

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
}

export interface NuevoPracticante {
  nombreCompleto: string;
  correo: string;
  carrera: string;
  semestre: number;
  hojaDeVida: File;
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
