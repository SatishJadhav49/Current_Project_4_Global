import {
  Component,
  HostListener,
  OnInit,
  QueryList,
  ViewChildren,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CorrelationService } from '../correlation.service';
import { CrModel, CrParameter } from '../correlation.model';
import { ToastService } from '../../../services';
import { CrDateRange, CrReport, CrReportSeries } from './report.model';
import { CHART_TYPES, ChartTypeKey } from './chart-types';
import {
  DATE_PRESETS,
  DatePresetKey,
  MAX_RANGE_DAYS,
  daysBetween,
  endOfDay,
  fromIsoDate,
  resolvePreset,
  startOfDay,
  toIsoDate,
} from './date-presets';
import { XChartComponent } from './x-chart.component';
import { ReportExportService } from './report-export.service';

@Component({
  selector: 'app-correlation-report',
  standalone: true,
  imports: [CommonModule, FormsModule, XChartComponent],
  templateUrl: './correlation-report.component.html',
})
export class CorrelationReportComponent implements OnInit {
  @ViewChildren('cardChart') cardCharts?: QueryList<XChartComponent>;

  // Filters
  models: CrModel[] = [];
  selectedModelId: number | null = null;
  modelsLoading = false;

  parameters: CrParameter[] = [];
  selectedParameterId: number | null = null;
  parametersLoading = false;

  readonly datePresets = DATE_PRESETS;
  datePreset: DatePresetKey = 'last7';
  customFrom = '';
  customTo = '';
  readonly todayIso = toIsoDate(new Date());

  // Report
  report: CrReport | null = null;
  range: CrDateRange | null = null;
  rangeText = '';
  loading = false;

  readonly chartTypes = CHART_TYPES;
  activeChart: ChartTypeKey = 'xchart';

  zoomSeries: CrReportSeries | null = null;
  exporting: 'excel' | 'pdf' | null = null;

  private readonly modelStorageKey = 'CR_Model_ID';

  private readonly correlationService = inject(CorrelationService);
  private readonly exportService = inject(ReportExportService);
  private readonly toastService = inject(ToastService);

  ngOnInit(): void {
    const { from, to } = resolvePreset('last7');
    this.customFrom = toIsoDate(from);
    this.customTo = toIsoDate(to);
    this.loadModels();
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closeZoom();
  }

  //*************************************** Filters ***************************************//

  private loadModels(): void {
    this.modelsLoading = true;
    this.correlationService.getModels().subscribe({
      next: (models) => {
        this.models = models ?? [];
        this.modelsLoading = false;

        const remembered = Number(this.readStorage(this.modelStorageKey));
        if (remembered && this.models.some((m) => m.Model_ID === remembered)) {
          this.selectedModelId = remembered;
          this.loadParameters();
        }
      },
      error: () => {
        this.modelsLoading = false;
      },
    });
  }

  onModelChange(): void {
    this.selectedParameterId = null;
    this.parameters = [];
    this.report = null;

    if (this.selectedModelId) {
      this.writeStorage(this.modelStorageKey, String(this.selectedModelId));
      this.loadParameters();
    }
  }

  private loadParameters(): void {
    if (!this.selectedModelId) return;

    this.parametersLoading = true;
    this.correlationService.getParameters(this.selectedModelId).subscribe({
      next: (parameters) => {
        this.parameters = parameters ?? [];
        this.parametersLoading = false;
      },
      error: () => {
        this.parametersLoading = false;
      },
    });
  }

  selectPreset(key: DatePresetKey): void {
    this.datePreset = key;
  }

  /** Resolved range of the current filter, or an error message. */
  private currentRange(): { from: Date; to: Date } | string {
    if (this.datePreset !== 'custom') {
      return resolvePreset(this.datePreset);
    }

    const from = fromIsoDate(this.customFrom);
    const to = fromIsoDate(this.customTo);
    if (!from || !to) return 'Select both start and end date.';
    if (from > to) return 'Start date must be before end date.';
    if (daysBetween(from, to) > MAX_RANGE_DAYS) return 'Date range cannot be more than 12 months.';
    return { from, to };
  }

  get presetRangeText(): string {
    const range = this.currentRange();
    return typeof range === 'string' ? '' : this.formatRange(range.from, range.to);
  }

  get canGenerate(): boolean {
    return !!this.selectedModelId && !!this.selectedParameterId && !this.loading;
  }

  //*************************************** Report ***************************************//

  generate(): void {
    if (!this.selectedParameterId) {
      this.toastService.showWarning('Parameter Required', 'Select a model and parameter.');
      return;
    }

    const range = this.currentRange();
    if (typeof range === 'string') {
      this.toastService.showWarning('Date Range', range);
      return;
    }

    this.loading = true;
    this.closeZoom();

    this.correlationService
      .getReport(this.selectedParameterId, toIsoDate(range.from), toIsoDate(range.to))
      .subscribe({
        next: (report) => {
          this.loading = false;
          this.report = report ?? null;
          this.range = { from: startOfDay(range.from), to: endOfDay(range.to) };
          this.rangeText = this.formatRange(range.from, range.to);
        },
        error: () => {
          this.loading = false;
        },
      });
  }

  get series(): CrReportSeries[] {
    return this.report?.Series ?? [];
  }

  get totalReadings(): number {
    return this.series.reduce((total, s) => total + (s.Stats?.Count ?? 0), 0);
  }

  get totalOutOfSpec(): number {
    return this.series.reduce((total, s) => total + (s.Stats?.Out_Of_Spec_Count ?? 0), 0);
  }

  get applicationCount(): number {
    return new Set(this.series.map((s) => s.App_ID)).size;
  }

  specText(series: CrReportSeries): string {
    return this.exportService.specText(series);
  }

  formatNumber(value: number | null | undefined): string {
    if (value === null || value === undefined) return '-';
    return Number(Number(value).toFixed(3)).toString();
  }

  //*************************************** Zoom ***************************************//

  openZoom(series: CrReportSeries): void {
    if (series.Points.length) {
      this.zoomSeries = series;
    }
  }

  closeZoom(): void {
    this.zoomSeries = null;
  }

  //*************************************** Export ***************************************//

  async exportExcel(): Promise<void> {
    if (!this.report || this.exporting) return;

    this.exporting = 'excel';
    try {
      await this.exportService.exportExcel(this.report, this.rangeText);
    } catch (error) {
      console.error('Correlation Excel export failed', error);
      this.toastService.showError('Export Failed', 'Unable to create the Excel file.');
    } finally {
      this.exporting = null;
    }
  }

  async exportPdf(): Promise<void> {
    if (!this.report || this.exporting) return;

    this.exporting = 'pdf';
    try {
      const images = new Map<number, string>();
      for (const chart of this.cardCharts?.toArray() ?? []) {
        const image = await chart.getImage();
        if (image) images.set(chart.mappingId, image);
      }
      await this.exportService.exportPdf(this.report, this.rangeText, images);
    } catch (error) {
      console.error('Correlation PDF export failed', error);
      this.toastService.showError('Export Failed', 'Unable to create the PDF file.');
    } finally {
      this.exporting = null;
    }
  }

  //*************************************** Helpers ***************************************//

  private formatRange(from: Date, to: Date): string {
    const format = (d: Date) =>
      d.toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' });
    return `${format(from)} – ${format(to)}`;
  }

  private readStorage(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private writeStorage(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // storage unavailable - only a convenience
    }
  }
}
