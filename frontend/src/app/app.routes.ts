import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

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
  { path: '**', redirectTo: 'registro' },
];
