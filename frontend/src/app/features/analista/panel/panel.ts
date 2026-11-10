import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ETIQUETAS_ESTADO,
  EstadoPracticante,
  Practicante,
} from '../../../core/models/practicante.model';
import { PracticantesService } from '../../../core/services/practicantes.service';
import { interpretarError } from '../../../core/utils/errores-http';

type Filtro = EstadoPracticante | 'Todos';
type Orden = 'recientes' | 'antiguos' | 'puntaje';

interface Indicador {
  filtro: Filtro;
  etiqueta: string;
  cantidad: number;
}

@Component({
  selector: 'app-panel',
  imports: [DatePipe, RouterLink],
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
  protected readonly orden = signal<Orden>('recientes');

  protected readonly indicadores = computed<Indicador[]>(() => {
    const lista = this.practicantes();
    const contar = (estado: EstadoPracticante) => lista.filter((p) => p.estado === estado).length;
    return [
      { filtro: 'Todos', etiqueta: 'Registrados', cantidad: lista.length },
      { filtro: 'Pendiente', etiqueta: 'Por evaluar', cantidad: contar('Pendiente') },
      { filtro: 'Viable', etiqueta: 'Viables', cantidad: contar('Viable') },
      { filtro: 'NoViable', etiqueta: 'No viables', cantidad: contar('NoViable') },
    ];
  });

  /** Puntaje promedio de las hojas de vida ya evaluadas con la rúbrica. */
  protected readonly puntajePromedio = computed(() => {
    const puntajes = this.practicantes()
      .map((p) => p.evaluacion?.puntajeTotal)
      .filter((p): p is number => p !== undefined);
    return puntajes.length
      ? Math.round(puntajes.reduce((a, b) => a + b, 0) / puntajes.length)
      : null;
  });

  protected readonly visibles = computed(() => {
    const filtro = this.filtro();
    const texto = normalizar(this.busqueda());
    const lista = this.practicantes().filter(
      (p) =>
        (filtro === 'Todos' || p.estado === filtro) &&
        (!texto || normalizar(`${p.nombreCompleto} ${p.correo} ${p.carrera}`).includes(texto)),
    );

    switch (this.orden()) {
      case 'antiguos':
        return [...lista].reverse();
      case 'puntaje':
        return [...lista].sort(
          (a, b) => (b.evaluacion?.puntajeTotal ?? -1) - (a.evaluacion?.puntajeTotal ?? -1),
        );
      default:
        return lista;
    }
  });

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

  protected buscar(evento: Event): void {
    this.busqueda.set((evento.target as HTMLInputElement).value);
  }

  protected ordenar(evento: Event): void {
    this.orden.set((evento.target as HTMLSelectElement).value as Orden);
  }

  protected claseEstado(estado: EstadoPracticante): string {
    return `insignia insignia--${estado.toLowerCase()}`;
  }

  protected clasePuntaje(puntaje: number): string {
    return puntaje >= 70 ? 'alto' : puntaje >= 50 ? 'medio' : 'bajo';
  }
}

function normalizar(texto: string): string {
  // Quita las tildes: "Díaz" → "Diaz" (con NFD, cada tilde queda como una marca aparte).
  return texto.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase().trim();
}
