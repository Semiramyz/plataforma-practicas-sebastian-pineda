import { DatePipe, DecimalPipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { ConCambiosSinGuardar } from '../../../core/auth/cambios-sin-guardar.guard';
import {
  CRITERIOS,
  Calificacion,
  CriterioRubrica,
  DecisionAnalista,
  ETIQUETAS_ESTADO,
  ETIQUETAS_MOTIVO,
  LONGITUD_MAXIMA_OBSERVACIONES,
  LONGITUD_MINIMA_JUSTIFICACION,
  MOTIVOS,
  MotivoRechazo,
  NIVELES,
  NuevoBorrador,
  Practicante,
  promedio,
  puntajeTotal,
  recomendacion,
} from '../../../core/models/practicante.model';
import { PracticantesService } from '../../../core/services/practicantes.service';
import { formatearTamano, interpretarError } from '../../../core/utils/errores-http';
import { EstadoAutoguardado, RETARDO_AUTOGUARDADO_MS } from './autoguardado';

type EstadoPdf = 'cargando' | 'listo' | 'error';

@Component({
  selector: 'app-evaluacion',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './evaluacion.html',
  styleUrl: './evaluacion.css',
})
export class EvaluacionPagina implements ConCambiosSinGuardar {
  private readonly servicio = inject(PracticantesService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly router = inject(Router);
  private readonly retardoAutoguardado = inject(RETARDO_AUTOGUARDADO_MS);
  private readonly cajaPendientes = viewChild<ElementRef<HTMLElement>>('cajaPendientes');

  /** Parámetro de ruta :id. */
  readonly id = input.required<string>();

  protected readonly criterios = CRITERIOS;
  protected readonly niveles = NIVELES;
  protected readonly motivosDisponibles = MOTIVOS;
  protected readonly etiquetasEstado = ETIQUETAS_ESTADO;
  protected readonly etiquetasMotivo = ETIQUETAS_MOTIVO;
  protected readonly maximoObservaciones = LONGITUD_MAXIMA_OBSERVACIONES;
  protected readonly puntajes = [1, 2, 3, 4, 5] as const;

  // ---------- Datos ----------
  protected readonly practicante = signal<Practicante | null>(null);
  protected readonly cargando = signal(true);
  protected readonly errorCarga = signal<string | null>(null);

  // ---------- Visor de la hoja de vida ----------
  protected readonly estadoPdf = signal<EstadoPdf>('cargando');
  protected readonly urlPdfSegura = signal<SafeResourceUrl | null>(null);
  private urlPdf: string | null = null;

  // ---------- Formulario de evaluación ----------
  protected readonly editando = signal(false);
  protected readonly calificacion = signal<Partial<Calificacion>>({});
  protected readonly decision = signal<DecisionAnalista | null>(null);
  protected readonly motivos = signal<MotivoRechazo[]>([]);
  protected readonly observaciones = signal('');

  // ---------- Guardado automático del avance (borrador en la base de datos) ----------
  protected readonly estadoAutoguardado = signal<EstadoAutoguardado>('inactivo');
  protected readonly borradorGuardadoEn = signal<string | null>(null);
  protected readonly borradorRecuperadoDe = signal<string | null>(null);
  private temporizadorAutoguardado: ReturnType<typeof setTimeout> | undefined;
  /** Guardados automáticos que todavía no reciben respuesta del servidor. */
  private borradoresEnCurso = 0;
  private guardarTrasBorrador = false;

  /** Se activa al intentar guardar con secciones incompletas: resalta lo que falta. */
  protected readonly mostrarPendientes = signal(false);

  protected readonly guardando = signal(false);
  protected readonly errorGuardado = signal<string | null>(null);
  protected readonly mensajeExito = signal<string | null>(null);
  protected readonly siguientePendiente = signal<number | null>(null);

  protected readonly calificados = computed(
    () => CRITERIOS.filter((c) => this.calificacion()[c.clave] !== undefined).length,
  );

  /** Calificación completa o null mientras falte algún criterio. */
  protected readonly calificacionCompleta = computed<Calificacion | null>(() =>
    this.calificados() === CRITERIOS.length ? (this.calificacion() as Calificacion) : null,
  );

  protected readonly puntaje = computed(() => {
    const c = this.calificacionCompleta();
    return c ? puntajeTotal(c) : null;
  });

  protected readonly promedio = computed(() => {
    const c = this.calificacionCompleta();
    return c ? promedio(c) : null;
  });

  protected readonly recomendacion = computed(() => {
    const c = this.calificacionCompleta();
    return c ? recomendacion(c) : null;
  });

  /** Explica qué estándar de calidad hizo que la rúbrica recomiende "No viable". */
  protected readonly razonesRecomendacion = computed(() => {
    const c = this.calificacionCompleta();
    if (!c) {
      return [];
    }
    const razones: string[] = [];
    if (c.ortografia <= 2) {
      razones.push('La ortografía y redacción no alcanzan el mínimo (3).');
    }
    const enUno = CRITERIOS.filter((criterio) => c[criterio.clave] === 1).map((x) => x.nombre);
    if (enUno.length > 0) {
      razones.push(`Calificación deficiente en: ${enUno.join(', ')}.`);
    }
    if (promedio(c) < 3) {
      razones.push('El promedio es menor que 3.');
    }
    return razones;
  });

  protected readonly contradiceRecomendacion = computed(
    () => !!this.decision() && !!this.recomendacion() && this.decision() !== this.recomendacion(),
  );

  protected readonly minimoObservaciones = computed(() => {
    if (this.contradiceRecomendacion()) {
      return LONGITUD_MINIMA_JUSTIFICACION;
    }
    return this.motivos().includes('Otro') ? 1 : 0;
  });

  /** Lo que falta para poder guardar la evaluación; al intentar guardar se muestra resaltado. */
  protected readonly pendientes = computed(() => {
    const pendientes: string[] = [];
    const faltan = CRITERIOS.length - this.calificados();
    if (faltan > 0) {
      pendientes.push(
        `Califica ${faltan === 1 ? 'el criterio restante' : `los ${faltan} criterios restantes`} de la rúbrica.`,
      );
    }
    if (!this.decision()) {
      pendientes.push('Elige la decisión: Viable o No viable.');
    }
    if (this.decision() === 'NoViable' && this.motivos().length === 0) {
      pendientes.push('Selecciona al menos un motivo de rechazo.');
    }
    const texto = this.observaciones().trim();
    if (texto.length > LONGITUD_MAXIMA_OBSERVACIONES) {
      pendientes.push(`Las observaciones superan ${LONGITUD_MAXIMA_OBSERVACIONES} caracteres.`);
    } else if (this.contradiceRecomendacion() && texto.length < LONGITUD_MINIMA_JUSTIFICACION) {
      pendientes.push(
        `Justifica en las observaciones por qué tu decisión difiere de la recomendación (mínimo ${LONGITUD_MINIMA_JUSTIFICACION} caracteres).`,
      );
    } else if (this.motivos().includes('Otro') && texto.length === 0) {
      pendientes.push("Explica el motivo 'Otro' en las observaciones.");
    }
    return pendientes;
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      // Si quedó un cambio esperando el guardado automático, se envía antes de salir.
      this.guardarBorradorAhora();
      this.liberarPdf();
    });
    // Se recarga cada vez que cambia el :id (por ejemplo, al pasar al siguiente pendiente).
    effect(() => {
      this.id();
      untracked(() => this.cargar());
    });
  }

  /**
   * Usado por el guard al salir de la pantalla. Lo pendiente se envía de inmediato a la base de
   * datos, así que solo se pide confirmación si el último guardado automático falló.
   */
  tieneCambiosSinGuardar(): boolean {
    // Se mira el estado ANTES de reintentar: si el último guardado falló, el avance no está a salvo.
    const ultimoGuardadoFallo = this.estadoAutoguardado() === 'error';
    this.guardarBorradorAhora();
    return ultimoGuardadoFallo;
  }

  /** Aviso del navegador si se intenta cerrar o recargar la pestaña con avance sin guardar. */
  @HostListener('window:beforeunload', ['$event'])
  protected alSalirDelNavegador(evento: BeforeUnloadEvent): void {
    const estado = this.estadoAutoguardado();
    if (estado === 'pendiente' || estado === 'guardando' || estado === 'error') {
      this.guardarBorradorAhora();
      evento.preventDefault();
    }
  }

  protected cargar(): void {
    const id = Number(this.id());
    if (!Number.isInteger(id) || id <= 0) {
      this.errorCarga.set('El practicante solicitado no existe.');
      this.cargando.set(false);
      return;
    }

    this.cargando.set(true);
    this.errorCarga.set(null);
    this.mensajeExito.set(null);
    this.siguientePendiente.set(null);
    this.urlPdfSegura.set(null);
    this.servicio.obtener(id).subscribe({
      next: (practicante) => {
        this.practicante.set(practicante);
        this.cargando.set(false);
        // Si hay un avance guardado, se abre directamente en modo edición para continuarlo.
        this.prepararFormulario(
          practicante,
          practicante.estado === 'Pendiente' || practicante.borrador !== null,
        );
        this.cargarPdf(id);
      },
      error: (error: unknown) => {
        this.errorCarga.set(interpretarError(error).mensaje);
        this.cargando.set(false);
      },
    });
  }

  // ---------- Acciones del formulario ----------

  protected calificar(criterio: CriterioRubrica, valor: number): void {
    this.calificacion.update((actual) => ({ ...actual, [criterio]: valor }));
    this.marcarCambio();
  }

  protected elegirDecision(decision: DecisionAnalista): void {
    if (!this.calificacionCompleta()) {
      return; // No se puede decidir sin haber calificado toda la rúbrica.
    }
    this.decision.set(decision);
    if (decision === 'Viable') {
      this.motivos.set([]);
    } else if (this.motivos().length === 0) {
      this.motivos.set(this.motivosSugeridos());
    }
    this.marcarCambio();
  }

  protected alternarMotivo(motivo: MotivoRechazo, marcado: boolean): void {
    this.motivos.update((actuales) =>
      marcado ? [...actuales, motivo] : actuales.filter((m) => m !== motivo),
    );
    this.marcarCambio();
  }

  protected escribirObservaciones(evento: Event): void {
    this.observaciones.set((evento.target as HTMLTextAreaElement).value);
    this.marcarCambio();
  }

  protected editar(): void {
    const practicante = this.practicante();
    if (practicante) {
      this.prepararFormulario(practicante, true);
      this.mensajeExito.set(null);
    }
  }

  /**
   * Cancelar la edición: si hay avance (guardado o por guardar) se pide confirmar y se descarta;
   * si no, se vuelve al resumen de la evaluación guardada.
   */
  protected cancelarEdicion(): void {
    const practicante = this.practicante();
    if (!practicante) {
      return;
    }
    if (this.hayAvance()) {
      this.descartarAvance();
      return;
    }
    this.prepararFormulario(practicante, practicante.estado === 'Pendiente');
  }

  /** ¿Hay algo que descartar? (un avance ya guardado o uno a punto de guardarse). */
  protected readonly hayAvance = computed(
    () => !!this.practicante()?.borrador || this.estadoAutoguardado() !== 'inactivo',
  );

  /** Borra el avance de la base de datos y vuelve a la última evaluación guardada (o a cero). */
  protected descartarAvance(): void {
    const practicante = this.practicante();
    if (
      !practicante ||
      !window.confirm('¿Descartar el avance de esta evaluación? No se puede deshacer.')
    ) {
      return;
    }

    clearTimeout(this.temporizadorAutoguardado);
    this.estadoAutoguardado.set('inactivo');
    this.servicio.descartarBorrador(practicante.id).subscribe({ error: () => undefined });
    const sinBorrador = { ...practicante, borrador: null };
    this.practicante.set(sinBorrador);
    this.prepararFormulario(sinBorrador, sinBorrador.estado === 'Pendiente');
  }

  protected guardar(): void {
    const practicante = this.practicante();
    const calificacion = this.calificacionCompleta();
    const decision = this.decision();
    if (!practicante || !calificacion || !decision || this.pendientes().length > 0) {
      // No se guarda una decisión incompleta: se explica qué falta en lugar de no hacer nada.
      this.mostrarPendientes.set(true);
      queueMicrotask(() => this.cajaPendientes()?.nativeElement.focus());
      return;
    }

    // Si un guardado automático va en camino, se espera a que termine: así el borrador no puede
    // llegar al servidor después de la evaluación definitiva y "revivir".
    if (this.borradoresEnCurso > 0) {
      this.guardarTrasBorrador = true;
      this.guardando.set(true);
      return;
    }

    // La evaluación definitiva reemplaza al borrador: se cancela cualquier guardado automático.
    clearTimeout(this.temporizadorAutoguardado);
    this.estadoAutoguardado.set('inactivo');
    this.guardando.set(true);
    this.errorGuardado.set(null);
    this.servicio
      .evaluar(practicante.id, {
        decision,
        calificacion,
        motivosRechazo: decision === 'NoViable' ? this.motivos() : [],
        observaciones: this.observaciones().trim() || null,
      })
      .subscribe({
        next: (actualizado) => {
          this.guardando.set(false);
          this.practicante.set(actualizado);
          this.prepararFormulario(actualizado, false);
          this.mensajeExito.set(
            `Evaluación guardada: ${actualizado.nombreCompleto} quedó como "${ETIQUETAS_ESTADO[actualizado.estado]}".`,
          );
          this.buscarSiguientePendiente(actualizado.id);
        },
        error: (error: unknown) => {
          this.guardando.set(false);
          const { mensaje, campos } = interpretarError(error);
          const detalles = Object.values(campos);
          this.errorGuardado.set(detalles.length > 0 ? detalles.join(' ') : mensaje);
        },
      });
  }

  protected irASiguiente(): void {
    const id = this.siguientePendiente();
    if (id) {
      void this.router.navigate(['/analista/practicantes', id]);
    }
  }

  // ---------- Hoja de vida ----------

  protected abrirPdf(): void {
    if (this.urlPdf) {
      window.open(this.urlPdf, '_blank', 'noopener');
    }
  }

  protected descargarPdf(): void {
    const practicante = this.practicante();
    if (this.urlPdf && practicante) {
      const enlace = document.createElement('a');
      enlace.href = this.urlPdf;
      enlace.download = practicante.hojaDeVidaNombre;
      enlace.click();
    }
  }

  // ---------- Ayudas para la plantilla ----------

  protected nivelElegido(criterio: CriterioRubrica): number | undefined {
    return this.calificacion()[criterio];
  }

  protected tamano(bytes: number): string {
    return formatearTamano(bytes);
  }

  protected claseEstado(estado: string): string {
    return `insignia insignia--${estado.toLowerCase()}`;
  }

  protected esSugerido(motivo: MotivoRechazo): boolean {
    return this.motivosSugeridos().includes(motivo);
  }

  // ---------- Privados ----------

  /**
   * Carga el formulario. En modo edición tiene prioridad el avance guardado (borrador); si no hay,
   * se parte de la última evaluación guardada, o de cero si el practicante está pendiente.
   */
  private prepararFormulario(practicante: Practicante, editando: boolean): void {
    clearTimeout(this.temporizadorAutoguardado);
    const borrador = editando ? practicante.borrador : null;
    const evaluacion = practicante.evaluacion;

    if (borrador) {
      const calificacion: Partial<Calificacion> = {};
      for (const criterio of CRITERIOS) {
        const valor = borrador.calificacion[criterio.clave];
        if (valor !== null && valor !== undefined) {
          calificacion[criterio.clave] = valor;
        }
      }
      this.calificacion.set(calificacion);
      this.decision.set(borrador.decision);
      this.motivos.set([...borrador.motivosRechazo]);
      this.observaciones.set(borrador.observaciones ?? '');
      this.borradorGuardadoEn.set(borrador.fechaActualizacionUtc);
      this.borradorRecuperadoDe.set(borrador.fechaActualizacionUtc);
    } else {
      this.calificacion.set(evaluacion ? { ...evaluacion.calificacion } : {});
      this.decision.set(practicante.estado === 'Pendiente' ? null : practicante.estado);
      this.motivos.set(evaluacion ? [...evaluacion.motivosRechazo] : []);
      this.observaciones.set(evaluacion?.observaciones ?? '');
      this.borradorGuardadoEn.set(null);
      this.borradorRecuperadoDe.set(null);
    }

    this.estadoAutoguardado.set('inactivo');
    this.mostrarPendientes.set(false);
    this.errorGuardado.set(null);
    this.editando.set(editando);
  }

  /** Motivos que corresponden a los criterios calificados con 2 o menos. */
  private motivosSugeridos(): MotivoRechazo[] {
    const c = this.calificacion();
    return [
      ...new Set(
        CRITERIOS.filter((criterio) => (c[criterio.clave] ?? 5) <= 2).map((x) => x.motivoSugerido),
      ),
    ];
  }

  /** Cada cambio en la evaluación programa el guardado automático del avance. */
  private marcarCambio(): void {
    this.mensajeExito.set(null);
    this.borradorRecuperadoDe.set(null);
    this.estadoAutoguardado.set('pendiente');
    clearTimeout(this.temporizadorAutoguardado);
    this.temporizadorAutoguardado = setTimeout(
      () => this.guardarBorradorAhora(),
      this.retardoAutoguardado,
    );
  }

  /** Guarda el avance en la base de datos si hay un cambio esperando. */
  protected guardarBorradorAhora(): void {
    const practicante = this.practicante();
    const estado = this.estadoAutoguardado();
    if (!practicante || !this.editando() || (estado !== 'pendiente' && estado !== 'error')) {
      return;
    }

    clearTimeout(this.temporizadorAutoguardado);
    this.estadoAutoguardado.set('guardando');
    this.borradoresEnCurso++;
    this.servicio.guardarBorrador(practicante.id, this.cuerpoBorrador()).subscribe({
      next: (borrador) => {
        this.borradoresEnCurso--;
        // Si hubo otro cambio mientras se guardaba, ese cambio sigue pendiente.
        if (this.estadoAutoguardado() === 'guardando') {
          this.estadoAutoguardado.set('guardado');
        }
        this.borradorGuardadoEn.set(borrador.fechaActualizacionUtc);
        this.practicante.update((p) => (p && p.id === practicante.id ? { ...p, borrador } : p));
        this.continuarGuardadoPendiente();
      },
      error: () => {
        this.borradoresEnCurso--;
        this.estadoAutoguardado.set('error');
        this.continuarGuardadoPendiente();
      },
    });
  }

  /** Si se pidió guardar la evaluación mientras viajaba un borrador, se guarda ahora. */
  private continuarGuardadoPendiente(): void {
    if (this.guardarTrasBorrador && this.borradoresEnCurso === 0) {
      this.guardarTrasBorrador = false;
      this.guardando.set(false);
      this.guardar();
    }
  }

  private cuerpoBorrador(): NuevoBorrador {
    return {
      ...this.calificacion(),
      decision: this.decision(),
      motivosRechazo: this.decision() === 'NoViable' ? this.motivos() : [],
      observaciones: this.observaciones() || null,
    };
  }

  private cargarPdf(id: number): void {
    this.estadoPdf.set('cargando');
    this.servicio.descargarHojaDeVida(id).subscribe({
      next: (pdf) => {
        this.liberarPdf();
        this.urlPdf = URL.createObjectURL(new Blob([pdf], { type: 'application/pdf' }));
        // Es un blob creado por esta misma página a partir de la API: se puede mostrar en el iframe.
        // El fragmento le pide al visor ajustar la página al ancho y ocultar las miniaturas.
        this.urlPdfSegura.set(
          this.sanitizer.bypassSecurityTrustResourceUrl(`${this.urlPdf}#navpanes=0&view=FitH`),
        );
        this.estadoPdf.set('listo');
      },
      error: () => this.estadoPdf.set('error'),
    });
  }

  private liberarPdf(): void {
    if (this.urlPdf) {
      URL.revokeObjectURL(this.urlPdf);
      this.urlPdf = null;
    }
  }

  private buscarSiguientePendiente(actual: number): void {
    this.siguientePendiente.set(null);
    this.servicio.listar().subscribe({
      next: (lista) => {
        const pendientes = lista.filter((p) => p.estado === 'Pendiente' && p.id !== actual);
        // La lista viene del más reciente al más antiguo: se atiende primero al más antiguo.
        this.siguientePendiente.set(pendientes.at(-1)?.id ?? null);
      },
      error: () => undefined,
    });
  }
}
