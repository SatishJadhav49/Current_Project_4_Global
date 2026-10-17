/** Contract every report chart component implements (used by the PDF export). */
export interface ReportChart {
  /** Mapping the chart belongs to. */
  readonly mappingId: number;
  /** PNG data URI of the rendered chart, or null when nothing is rendered. */
  getImage(): Promise<string | null>;
}

/** Colours shared by all report charts. */
export const CHART_COLORS = {
  reading: '#4f46e5',
  limit: '#e11d48',
  outOfSpec: '#dc2626',
};

export function formatValue(value: number | null | undefined): string {
  if (value === null || value === undefined || isNaN(value)) return '-';
  return Number(value.toFixed(3)).toString();
}
