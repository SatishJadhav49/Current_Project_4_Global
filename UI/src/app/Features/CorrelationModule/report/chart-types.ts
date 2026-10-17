/**
 * Graph tabs of the correlation report. To add a new graph type later
 * (e.g. Histogram): create its chart component, add an entry here and a
 * matching @case in correlation-report.component.html.
 */
export type ChartTypeKey = 'xchart';

export interface ChartType {
  key: ChartTypeKey;
  label: string;
  description: string;
}

export const CHART_TYPES: ChartType[] = [
  {
    key: 'xchart',
    label: 'X Chart',
    description: 'Individual readings over time against LSL / USL',
  },
];
