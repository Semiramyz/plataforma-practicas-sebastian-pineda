import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  DecisionAnalista,
  ETIQUETAS_ESTADO,
  EstadoPracticante,
  Practicante,
} from '../../../core/models/practicante.model';
import { PracticantesService } from '../../../core/services/practicantes.service';
import { formatearTamano, interpretarError } from '../../../core/utils/errores-http';

type Filtro = EstadoPracticante | 'Todos';

interface Indicador {
  filtro: Filtro;
  etiqueta: string;
  cantidad: number;
}

@Component({
  selector: 'app-panel',
  imports: [DatePipe],
  templateUrl: './panel.html',
  styleUrl: './panel.css',
})
export class Panel implements OnInit {
  private readonly servicio = inject(PracticantesService);

  protected readonly etiquetas = ETIQUETAS_ESTADO;

  protected readonly practicantes = signal<Practicante[]>([]);
  protected readonly cargando = signal(true);
  protected readonly errorCarga = signal<string | null>(null);
  protected readonly filtro = signal<Filtro>('Todos');
  protected readonly busqueda = signal('');
  protected readonly seleccionadoId = signal<number | null>(null);
  protected readonly guardando = signal<DecisionAnalista | null>(null);
  protected readonly descargando = signal(false);
  protected readonly mensaje = signal<{ tipo: 'exito' | 'error'; texto: string } | null>(null);

  protected readonly indicadores = computed<Indicador[]>(() => {
    const lista = this.practicantes();
    const contar = (estado: EstadoPracticante) => lista.filter((p) => p.estado === estado).length;
    return [
      { filtro: 'Todos', etiqueta: 'Registrados', cantidad: lista.length },
      { filtro: 'Pendiente', etiqueta: 'Pendientes', cantidad: contar('Pendiente') },
      { filtro: 'Viable', etiqueta: 'Viables', cantidad: contar('Viable') },
      { filtro: 'NoViable', etiqueta: 'No viables', cantidad: contar('NoViable') },
    ];
  });

  protected readonly visibles = computed(() => {
    const filtro = this.filtro();
    const texto = normalizar(this.busqueda());
    return this.practicantes().filter(
      (p) =>
        (filtro === 'Todos' || p.estado === filtro) &&
        (!texto || normalizar(`${p.nombreCompleto} ${p.correo} ${p.carrera}`).includes(texto)),
    );
  });

  protected readonly seleccionado = computed(
    () => this.practicantes().find((p) => p.id === this.seleccionadoId()) ?? null,
  );

  ngOnInit(): void {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.errorCarga.set(null);
    this.servicio.listar().subscribe({
      next: (lista) => {
        this.practicantes.set(lista);
        this.cargando.set(false);
      },
      error: (error: unknown) => {
        this.errorCarga.set(interpretarError(error).mensaje);
        this.cargando.set(false);
      },
    });
  }

  protected seleccionar(practicante: Practicante): void {
    this.seleccionadoId.set(practicante.id);
    this.mensaje.set(null);
  }

  protected cerrarDetalle(): void {
    this.seleccionadoId.set(null);
  }

  protected buscar(evento: Event): void {
    this.busqueda.set((evento.target as HTMLInputElement).value);
  }

  protected evaluar(practicante: Practicante, decision: DecisionAnalista): void {
    if (this.guardando() || practicante.estado === decision) {
      return;
    }

    this.guardando.set(decision);
    this.mensaje.set(null);
    this.servicio.actualizarEstado(practicante.id, decision).subscribe({
      next: (actualizado) => {
        this.practicantes.update((lista) =>
          lista.map((p) => (p.id === actualizado.id ? actualizado : p)),
        );
        this.guardando.set(null);
        this.mensaje.set({
          tipo: 'exito',
          texto: `${actualizado.nombreCompleto} quedó marcado como "${this.etiquetas[actualizado.estado]}".`,
        });
      },
      error: (error: unknown) => {
        this.guardando.set(null);
        this.mensaje.set({ tipo: 'error', texto: interpretarError(error).mensaje });
      },
    });
  }

  /** Abre la hoja de vida en una pestaña nueva o la descarga con su nombre original. */
  protected hojaDeVida(practicante: Practicante, modo: 'ver' | 'descargar'): void {
    // La pestaña se abre de inmediato para que el navegador no la bloquee como ventana emergente.
    const pestana = modo === 'ver' ? window.open('', '_blank') : null;
    this.descargando.set(true);

    this.servicio.descargarHojaDeVida(practicante.id).subscribe({
      next: (pdf) => {
        this.descargando.set(false);
        const url = URL.createObjectURL(pdf);
        if (pestana) {
          pestana.location.href = url;
        } else {
          const enlace = document.createElement('a');
          enlace.href = url;
          enlace.download = practicante.hojaDeVidaNombre;
          enlace.click();
        }
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (error: unknown) => {
        pestana?.close();
        this.descargando.set(false);
        this.mensaje.set({ tipo: 'error', texto: interpretarError(error).mensaje });
      },
    });
  }

  protected claseEstado(estado: EstadoPracticante): string {
    return `insignia insignia--${estado.toLowerCase()}`;
  }

  protected tamano(bytes: number): string {
    return formatearTamano(bytes);
  }
}

function normalizar(texto: string): string {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}
