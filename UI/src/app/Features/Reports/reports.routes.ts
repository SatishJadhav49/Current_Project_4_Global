import { Routes } from '@angular/router';

export const ReportsRoutes: Routes = [
  {
    path: 'globalsearch',
    loadComponent: () =>
      import('./globalsearch/globalsearch.component').then(
        (m) => m.GlobalsearchComponent
      ),
  },
];
