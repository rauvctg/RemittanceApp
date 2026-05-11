import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/main-layout.component').then(m => m.MainLayoutComponent),
    children: [
      {
        path: 'login',
        canActivate: [guestGuard],
        loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        canActivate: [guestGuard],
        loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
      },
      {
        path: 'track',
        loadComponent: () => import('./features/track/track.component').then(m => m.TrackComponent)
      },
      { path: '', pathMatch: 'full', redirectTo: 'remittances' },
      {
        path: 'remittances',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/remittances/remittance-list.component').then(m => m.RemittanceListComponent)
      },
      {
        path: 'remittances/new',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/remittances/remittance-form.component').then(m => m.RemittanceFormComponent)
      },
      {
        path: 'remittances/:id/edit',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/remittances/remittance-form.component').then(m => m.RemittanceFormComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
