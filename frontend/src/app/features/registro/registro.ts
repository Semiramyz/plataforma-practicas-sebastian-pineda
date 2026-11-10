import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { RegistroPracticante } from '../../core/models/practicante.model';
import { PracticantesService } from '../../core/services/practicantes.service';
import { formatearTamano, interpretarError } from '../../core/utils/errores-http';
import {
  TAMANO_MAXIMO_HOJA_DE_VIDA,
  archivoPdf,
  correoCompleto,
  noVacio,
  nombrePersona,
  textoCarrera,
} from '../../core/validators/formulario.validators';

type Campo =
  'nombreCompleto' | 'correo' | 'carrera' | 'semestre' | 'hojaDeVida' | 'autorizaTratamientoDatos';

const MENSAJES: Record<Campo, Record<string, string>> = {
  nombreCompleto: {
    required: 'Escribe tu nombre completo.',
    minlength: 'El nombre debe tener al menos 3 caracteres.',
    maxlength: 'El nombre no puede superar 150 caracteres.',
    caracteresNombre: 'El nombre solo puede contener letras, espacios, apóstrofes y guiones.',
    nombreIncompleto: 'Escribe tu nombre y al menos un apellido.',
    letrasRepetidas: 'El nombre tiene letras repetidas de forma inusual; revísalo.',
  },
  correo: {
    required: 'Escribe tu correo electrónico.',
    email: 'Escribe un correo válido, por ejemplo nombre@correo.com.',
    maxlength: 'El correo no puede superar 254 caracteres.',
  },
  carrera: {
    required: 'Escribe tu carrera universitaria.',
    minlength: 'La carrera debe tener al menos 2 caracteres.',
    maxlength: 'La carrera no puede superar 150 caracteres.',
    caracteresCarrera:
      'La carrera solo puede contener letras, números y signos básicos de puntuación.',
  },
  autorizaTratamientoDatos: {
    required: 'Debes autorizar el tratamiento de tus datos personales para registrarte.',
  },
  semestre: {
    required: 'Selecciona tu semestre actual.',
    min: 'El semestre debe estar entre 1 y 12.',
    max: 'El semestre debe estar entre 1 y 12.',
  },
  hojaDeVida: {
    required: 'Adjunta tu hoja de vida en PDF.',
    tipoArchivo: 'La hoja de vida debe ser un archivo PDF.',
    archivoVacio: 'El archivo está vacío.',
    tamanoArchivo: `El archivo no puede superar ${formatearTamano(TAMANO_MAXIMO_HOJA_DE_VIDA)}.`,
  },
};

const PARTICULAS = new Set(['de', 'del', 'la', 'las', 'los', 'y', 'e', 'da', 'van', 'von']);

/** "ANA DE LA ROSA" o "ana de la rosa" → "Ana de la Rosa" (igual que la API). */
export function formatoNombrePropio(nombre: string): string {
  const limpio = nombre.trim().replace(/\s+/g, ' ');
  if (limpio !== limpio.toUpperCase() && limpio !== limpio.toLowerCase()) {
    return limpio;
  }
  return limpio
    .toLocaleLowerCase('es-CO')
    .split(' ')
    .map((palabra, i) =>
      i > 0 && PARTICULAS.has(palabra)
        ? palabra
        : palabra.charAt(0).toLocaleUpperCase('es-CO') + palabra.slice(1),
    )
    .join(' ');
}

@Component({
  selector: 'app-registro',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './registro.html',
  styleUrl: './registro.css',
})
export class Registro {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly servicio = inject(PracticantesService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly selectorArchivo = viewChild<ElementRef<HTMLInputElement>>('selectorArchivo');

  protected readonly semestres = Array.from({ length: 12 }, (_, i) => i + 1);
  protected readonly carrerasSugeridas = [
    'Ingeniería de Sistemas',
    'Ingeniería de Software',
    'Ingeniería Electrónica',
    'Ingeniería Industrial',
    'Ciencia de Datos',
    'Administración de Empresas',
    'Diseño de Comunicación',
  ];
  protected readonly tamanoMaximo = formatearTamano(TAMANO_MAXIMO_HOJA_DE_VIDA);

  protected readonly formulario = this.fb.group({
    nombreCompleto: [
      '',
      [
        Validators.required,
        noVacio,
        Validators.minLength(3),
        Validators.maxLength(150),
        nombrePersona,
      ],
    ],
    correo: ['', [Validators.required, correoCompleto, Validators.maxLength(254)]],
    carrera: [
      '',
      [
        Validators.required,
        noVacio,
        Validators.minLength(2),
        Validators.maxLength(150),
        textoCarrera,
      ],
    ],
    semestre: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(1),
      Validators.max(12),
    ]),
    hojaDeVida: this.fb.control<File | null>(null, [Validators.required, archivoPdf()]),
    autorizaTratamientoDatos: [false, Validators.requiredTrue],
  });

  protected readonly enviando = signal(false);
  protected readonly errorGeneral = signal<string | null>(null);
  protected readonly erroresServidor = signal<Partial<Record<Campo, string>>>({});
  protected readonly registro = signal<RegistroPracticante | null>(null);
  protected readonly arrastrando = signal(false);

  protected error(campo: Campo): string | null {
    const control = this.formulario.controls[campo];
    const servidor = this.erroresServidor()[campo];
    if (servidor) {
      return servidor;
    }
    if (!control.errors || !(control.touched || control.dirty)) {
      return null;
    }
    const clave = Object.keys(control.errors)[0];
    return MENSAJES[campo][clave] ?? 'Valor no válido.';
  }

  protected archivo(): File | null {
    return this.formulario.controls.hojaDeVida.value;
  }

  protected tamano(bytes: number): string {
    return formatearTamano(bytes);
  }

  protected alSeleccionarArchivo(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.asignarArchivo(input.files?.[0] ?? null);
  }

  protected alArrastrar(evento: DragEvent, activo: boolean): void {
    evento.preventDefault();
    this.arrastrando.set(activo);
  }

  protected alSoltar(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastrando.set(false);
    this.asignarArchivo(evento.dataTransfer?.files[0] ?? null);
  }

  protected quitarArchivo(): void {
    this.asignarArchivo(null);
    const input = this.selectorArchivo()?.nativeElement;
    if (input) {
      input.value = '';
    }
  }

  protected limpiarErrorServidor(campo: Campo): void {
    if (this.erroresServidor()[campo]) {
      this.erroresServidor.update(({ [campo]: _, ...resto }) => resto);
    }
  }

  protected enviar(): void {
    this.errorGeneral.set(null);
    this.erroresServidor.set({});

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.enfocarPrimerError();
      return;
    }

    const datos = this.formulario.getRawValue();
    this.enviando.set(true);
    this.servicio
      .registrar({
        nombreCompleto: formatoNombrePropio(datos.nombreCompleto),
        correo: datos.correo.trim(),
        carrera: datos.carrera.trim().replace(/\s+/g, ' '),
        semestre: datos.semestre!,
        hojaDeVida: datos.hojaDeVida!,
        autorizaTratamientoDatos: datos.autorizaTratamientoDatos,
      })
      .subscribe({
        next: (registro) => {
          this.enviando.set(false);
          this.registro.set(registro);
        },
        error: (error: unknown) => {
          this.enviando.set(false);
          const { mensaje, campos } = interpretarError(error);
          this.errorGeneral.set(mensaje);
          this.erroresServidor.set(campos as Partial<Record<Campo, string>>);
        },
      });
  }

  protected registrarOtro(): void {
    this.registro.set(null);
    this.formulario.reset();
    this.quitarArchivo();
  }

  private asignarArchivo(archivo: File | null): void {
    const control = this.formulario.controls.hojaDeVida;
    control.setValue(archivo);
    control.markAsTouched();
    this.limpiarErrorServidor('hojaDeVida');
  }

  private enfocarPrimerError(): void {
    queueMicrotask(() =>
      this.host.nativeElement.querySelector<HTMLElement>('.ng-invalid[formControlName]')?.focus(),
    );
  }
}
