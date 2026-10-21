import {
  Component,
  ElementRef,
  HostListener,
  OnInit,
  QueryList,
  ViewChild,
  ViewChildren,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CorrelationService } from '../correlation.service';
import { CrModel, CrParameter } from '../correlation.model';
import { ToastService } from '../../../services';
import { CrReport, CrReportFilter, CrReportSeries } from './report.model';
import { CHART_TYPES, ChartTypeKey } from './chart-types';
import {
  DATE_PRESETS,
  DEFAULT_LAST_N,
  DatePresetKey,
  MAX_LAST_N,
  MAX_RANGE_DAYS,
  daysBetween,
  fromIsoDate,
  resolvePreset,
  toIsoDate,
} from './date-presets';
import { XChartComponent } from './x-chart.component';
import { HistogramChartComponent } from './histogram-chart.component';
import { ReportChart } from './report-chart';
import { ImageThumbComponent } from '../shared/image-thumb.component';
import { CrViewerImage, ImageViewerComponent } from '../shared/image-viewer.component';
import { ReportExportService } from './report-export.service';

@Component({
  selector: 'app-correlation-report',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    XChartComponent,
    HistogramChartComponent,
    ImageThumbComponent,
    ImageViewerComponent,
  ],
  templateUrl: './correlation-report.component.html',
})
export class CorrelationReportComponent implements OnInit {
  /** Charts of the active graph tab (X Chart / Histogram) - used by the PDF export. */
  @ViewChildren('cardChart') cardCharts?: QueryList<ReportChart>;
  @ViewChild('parameterPicker') parameterPicker?: ElementRef<HTMLElement>;
  @ViewChild('parameterSearchInput') parameterSearchInput?: ElementRef<HTMLInputElement>;

  // Filters
  models: CrModel[] = [];
  selectedModelId: number | null = null;
  modelsLoading = false;

  parameters: CrParameter[] = [];
  selectedParameterId: number | null = null;
  parametersLoading = false;
  parameterOpen = false;
  parameterSearch = '';

  readonly datePresets = DATE_PRESETS;
  datePreset: DatePresetKey = 'last7';
  customFrom = '';
  customTo = '';
  lastN: number | null = DEFAULT_LAST_N;
  readonly maxLastN = MAX_LAST_N;
  readonly todayIso = toIsoDate(new Date());

  // Report
  report: CrReport | null = null;
  filterText = '';
  loading = false;

  readonly chartTypes = CHART_TYPES;
  activeChart: ChartTypeKey = 'xchart';

  zoomSeries: CrReportSeries | null = null;
  viewerImage: CrViewerImage | null = null;
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
    // The image viewer sits on top - Esc closes only the viewer
    if (this.viewerImage) {
      this.viewerImage = null;
      return;
    }
    this.parameterOpen = false;
    this.closeZoom();
  }

  /** Closes the parameter dropdown on an outside click. */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (
      this.parameterOpen &&
      !this.parameterPicker?.nativeElement.contains(event.target as Node)
    ) {
      this.parameterOpen = false;
    }
  }

  //*************************************** Model / Parameter ***************************************//

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
    this.parameterSearch = '';
    this.parameterOpen = false;
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

  get selectedParameter(): CrParameter | undefined {
    return this.parameters.find((p) => p.CR_Parameter_ID === this.selectedParameterId);
  }

  get filteredParameters(): CrParameter[] {
    const search = this.parameterSearch.trim().toLowerCase();
    return search
      ? this.parameters.filter((p) => (p.Parameter_Name ?? '').toLowerCase().includes(search))
      : this.parameters;
  }

  get parameterPlaceholder(): string {
    if (this.parametersLoading) return 'Loading...';
    if (!this.selectedModelId) return 'Select a model first';
    return this.parameters.length ? 'Select a parameter' : 'No parameters for this model';
  }

  toggleParameterPicker(): void {
    if (!this.selectedModelId || this.parametersLoading || !this.parameters.length) return;

    this.parameterOpen = !this.parameterOpen;
    if (this.parameterOpen) {
      this.parameterSearch = '';
      setTimeout(() => this.parameterSearchInput?.nativeElement.focus());
    }
  }

  selectParameter(parameter: CrParameter): void {
    if (!parameter.Total_Mapped) return;
    this.selectedParameterId = parameter.CR_Parameter_ID;
    this.parameterOpen = false;
  }

  /** Enter in the search box picks the first selectable match. */
  selectFirstMatch(): void {
    const match = this.filteredParameters.find((p) => p.Total_Mapped > 0);
    if (match) this.selectParameter(match);
  }

  //*************************************** Period ***************************************//

  /** Filter for the API plus its display text, or an error message. */
  private currentFilter(): { filter: CrReportFilter; text: string } | string {
    if (this.datePreset === 'lastN') {
      const n = Number(this.lastN);
      if (!Number.isInteger(n) || n < 1 || n > MAX_LAST_N) {
        return `Enter a number of audits between 1 and ${MAX_LAST_N}.`;
      }
      return { filter: { lastN: n }, text: `Last ${n} audit${n === 1 ? '' : 's'}` };
    }

    let from: Date | null;
    let to: Date | null;

    if (this.datePreset === 'custom') {
      from = fromIsoDate(this.customFrom);
      to = fromIsoDate(this.customTo);
      if (!from || !to) return 'Select both start and end date.';
      if (from > to) return 'Start date must be before end date.';
      if (daysBetween(from, to) > MAX_RANGE_DAYS) return 'Date range cannot be more than 12 months.';
    } else {
      ({ from, to } = resolvePreset(this.datePreset));
    }

    return {
      filter: { fromDate: toIsoDate(from), toDate: toIsoDate(to) },
      text: this.formatRange(from, to),
    };
  }

  get periodText(): string {
    const current = this.currentFilter();
    return typeof current === 'string' ? '' : current.text;
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

    const current = this.currentFilter();
    if (typeof current === 'string') {
      this.toastService.showWarning('Period', current);
      return;
    }

    this.loading = true;
    this.closeZoom();

    this.correlationService.getReport(this.selectedParameterId, current.filter).subscribe({
      next: (report) => {
        this.loading = false;
        this.report = report ?? null;
        this.filterText = current.text;
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

  get activeChartLabel(): string {
    return this.chartTypes.find((c) => c.key === this.activeChart)?.label ?? '';
  }

  /** Small title above a card's chart, e.g. "X̄ Chart · Average of readings". */
  chartTitle(series: CrReportSeries): string {
    if (this.activeChart === 'histogram') {
      return series.Is_Average ? 'Histogram with Normal Curve · Average of readings' : 'Histogram with Normal Curve';
    }
    return series.Is_Average ? 'X̄ Chart · Average of readings' : 'X Chart · Individual readings';
  }

  get emptyReadingsText(): string {
    return this.report?.Last_N ? 'No audits found for this item' : 'No readings in the selected date range';
  }

  //*************************************** Images ***************************************//

  imageUrl(series: CrReportSeries): string {
    return this.correlationService.imageUrl(series.App_ID, series.Source_Leaf_ID);
  }

  openImage(series: CrReportSeries): void {
    this.viewerImage = {
      src: this.imageUrl(series),
      title: series.Display_Text,
      subtitle: [series.App_Name, series.Source_Leaf_Type].filter(Boolean).join(' · '),
    };
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
      await this.exportService.exportExcel(this.report, this.filterText);
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
      await this.exportService.exportPdf(this.report, this.filterText, images, this.activeChartLabel);
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
