/**
 * Graph tabs of the correlation report. To add a new graph type later
 * (e.g. Box plot): create its chart component (implementing ReportChart), add
 * an entry here and a matching @case (card + zoom modal) in
 * correlation-report.component.html.
 */
export type ChartTypeKey = 'xchart' | 'histogram';

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
  {
    key: 'histogram',
    label: 'Histogram',
    description: 'Distribution of readings against LSL / USL',
  },
];
