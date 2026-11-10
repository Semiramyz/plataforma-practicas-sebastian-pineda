import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BorradorEvaluacion,
  NuevaEvaluacion,
  NuevoBorrador,
  NuevoPracticante,
  Practicante,
  RegistroPracticante,
} from '../models/practicante.model';

@Injectable({ providedIn: 'root' })
export class PracticantesService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/practicantes';

  registrar(datos: NuevoPracticante): Observable<RegistroPracticante> {
    const formulario = new FormData();
    formulario.append('nombreCompleto', datos.nombreCompleto);
    formulario.append('correo', datos.correo);
    formulario.append('carrera', datos.carrera);
    formulario.append('semestre', String(datos.semestre));
    formulario.append('autorizaTratamientoDatos', String(datos.autorizaTratamientoDatos));
    formulario.append('hojaDeVida', datos.hojaDeVida, datos.hojaDeVida.name);
    return this.http.post<RegistroPracticante>(this.url, formulario);
  }

  listar(): Observable<Practicante[]> {
    return this.http.get<Practicante[]>(this.url);
  }

  obtener(id: number): Observable<Practicante> {
    return this.http.get<Practicante>(`${this.url}/${id}`);
  }

  evaluar(id: number, evaluacion: NuevaEvaluacion): Observable<Practicante> {
    return this.http.put<Practicante>(`${this.url}/${id}/evaluacion`, evaluacion);
  }

  /** Guarda el avance de la evaluación en la base de datos (guardado automático). */
  guardarBorrador(id: number, borrador: NuevoBorrador): Observable<BorradorEvaluacion> {
    return this.http.put<BorradorEvaluacion>(`${this.url}/${id}/evaluacion/borrador`, borrador);
  }

  descartarBorrador(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}/evaluacion/borrador`);
  }

  descargarHojaDeVida(id: number): Observable<Blob> {
    return this.http.get(`${this.url}/${id}/hoja-de-vida`, { responseType: 'blob' });
  }
}
