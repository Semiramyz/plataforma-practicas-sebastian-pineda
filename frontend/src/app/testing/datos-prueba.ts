import { Practicante } from '../core/models/practicante.model';

/** Practicantes de ejemplo: uno pendiente y uno evaluado como viable. */
export const PRACTICANTES: Practicante[] = [
  {
    id: 2,
    nombreCompleto: 'Carlos Díaz',
    correo: 'carlos@correo.com',
    carrera: 'Ingeniería Industrial',
    semestre: 6,
    estado: 'Pendiente',
    fechaRegistroUtc: '2026-11-08T15:00:00Z',
    fechaEvaluacionUtc: null,
    hojaDeVidaNombre: 'cv-carlos.pdf',
    hojaDeVidaTamanoBytes: 120_000,
    evaluacion: null,
    borrador: null,
    historial: [],
  },
  {
    id: 1,
    nombreCompleto: 'Laura Ríos',
    correo: 'laura@correo.com',
    carrera: 'Ingeniería de Sistemas',
    semestre: 8,
    estado: 'Viable',
    fechaRegistroUtc: '2026-11-07T15:00:00Z',
    fechaEvaluacionUtc: '2026-11-08T10:00:00Z',
    hojaDeVidaNombre: 'cv-laura.pdf',
    hojaDeVidaTamanoBytes: 80_000,
    evaluacion: {
      calificacion: {
        ortografia: 5,
        presentacion: 4,
        formacion: 4,
        experiencia: 4,
        habilidades: 4,
      },
      promedio: 4.2,
      puntajeTotal: 80,
      recomendacion: 'Viable',
      motivosRechazo: [],
      observaciones: null,
      evaluador: 'Analista de selección',
    },
    borrador: null,
    historial: [
      {
        decision: 'Viable',
        puntajeTotal: 80,
        evaluador: 'Analista de selección',
        fechaUtc: '2026-11-08T10:00:00Z',
      },
      {
        decision: 'NoViable',
        puntajeTotal: 45,
        evaluador: 'Analista de selección',
        fechaUtc: '2026-11-08T09:00:00Z',
      },
    ],
  },
];
