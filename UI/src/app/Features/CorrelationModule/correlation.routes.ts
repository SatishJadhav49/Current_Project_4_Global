import { Routes } from '@angular/router';

export const CorrelationRoutes: Routes = [
  {
    path: 'mapping-master',
    loadComponent: () =>
      import('./mapping-master/mapping-master.component').then(
        (m) => m.MappingMasterComponent
      ),
  },
  {
    path: 'mapping-master/:parameterId',
    loadComponent: () =>
      import('./parameter-mapping/parameter-mapping.component').then(
        (m) => m.ParameterMappingComponent
      ),
  },
  {
    // Shown under the Reports menu (MM_Sub_Menus.ActionName = 'correlation/correlationreport')
    path: 'correlationreport',
    loadComponent: () =>
      import('./report/correlation-report.component').then(
        (m) => m.CorrelationReportComponent
      ),
  },
  { path: '', redirectTo: 'mapping-master', pathMatch: 'full' },
];
