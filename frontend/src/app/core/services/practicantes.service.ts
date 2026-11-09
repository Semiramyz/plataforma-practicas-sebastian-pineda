import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  DecisionAnalista,
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
    formulario.append('hojaDeVida', datos.hojaDeVida, datos.hojaDeVida.name);
    return this.http.post<RegistroPracticante>(this.url, formulario);
  }

  listar(): Observable<Practicante[]> {
    return this.http.get<Practicante[]>(this.url);
  }

  actualizarEstado(id: number, estado: DecisionAnalista): Observable<Practicante> {
    return this.http.patch<Practicante>(`${this.url}/${id}/estado`, { estado });
  }

  descargarHojaDeVida(id: number): Observable<Blob> {
    return this.http.get(`${this.url}/${id}/hoja-de-vida`, { responseType: 'blob' });
  }
}
