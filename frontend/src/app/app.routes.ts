import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { cambiosSinGuardarGuard } from './core/auth/cambios-sin-guardar.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'registro' },
  {
    path: 'registro',
    title: 'Registro de practicantes',
    loadComponent: () => import('./features/registro/registro').then((m) => m.Registro),
  },
  {
    path: 'analista/login',
    title: 'Ingreso de analistas',
    loadComponent: () => import('./features/analista/login/login').then((m) => m.Login),
  },
  {
    path: 'analista',
    title: 'Panel de analista',
    canActivate: [authGuard],
    loadComponent: () => import('./features/analista/panel/panel').then((m) => m.Panel),
  },
  {
    path: 'analista/practicantes/:id',
    title: 'Evaluación de hoja de vida',
    canActivate: [authGuard],
    canDeactivate: [cambiosSinGuardarGuard],
    loadComponent: () =>
      import('./features/analista/evaluacion/evaluacion').then((m) => m.EvaluacionPagina),
  },
  { path: '**', redirectTo: 'registro' },
];
