import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import ApexCharts from 'apexcharts';
import { CrReadingPoint, CrReportSeries } from './report.model';
import { CHART_COLORS as COLORS, ReportChart, formatValue } from './report-chart';

type LimitKey = 'LSL' | 'USL';
type XYPoint = { x: number; y: number | null };

/**
 * X Chart (individual readings) of one mapped item.
 * The x axis is the reading sequence (1..n) labelled with the audit date, so only
 * dates that have data are shown. Multi-reading apps plot one point per audit
 * (average of Reading1..3, i.e. an X-bar chart); the readings are in the tooltip. Stepped LSL / USL lines are
 * labelled only at start and end; out-of-spec readings get red markers.
 */
@Component({
  selector: 'app-cr-x-chart',
  standalone: true,
  template: `<div #chartHost class="w-full"></div>`,
})
export class XChartComponent implements ReportChart, AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('chartHost', { static: true }) chartHost!: ElementRef<HTMLDivElement>;

  @Input({ required: true }) series!: CrReportSeries;
  @Input() height = 260;
  /** Zoom / pan toolbar - used in the zoom modal. */
  @Input() interactive = false;

  private chart?: ApexCharts;
  private viewReady = false;

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.render();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.viewReady && (changes['series'] || changes['height'])) {
      this.render();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  get mappingId(): number {
    return this.series?.Mapping_ID;
  }

  /** PNG of the chart for the PDF export. */
  async getImage(): Promise<string | null> {
    if (!this.chart) return null;
    const result = await this.chart.dataURI({ scale: 2 });
    return 'imgURI' in result ? result.imgURI : null;
  }

  private render(): void {
    this.chart?.destroy();
    this.chart = undefined;

    if (!this.series?.Points?.length) return;

    this.chart = new ApexCharts(this.chartHost.nativeElement, this.buildOptions());
    this.chart.render();
  }

  private buildOptions(): ApexCharts.ApexOptions {
    const points = this.series.Points;
    const count = points.length;
    const dateLabels = points.map((p) => this.dateLabel(p.Audit_Date));

    // x = position of the reading (1..n)
    const readingData: XYPoint[] = points.map((p, i) => ({ x: i + 1, y: p.Reading }));

    const chartSeries: ApexAxisChartSeries = [{ name: 'Reading', data: readingData }];
    const strokeCurves: ('straight' | 'stepline')[] = ['straight'];
    const dashes = [0];
    const widths = [1.6];
    const colors = [COLORS.reading];
    const markerSizes = [this.interactive ? 4 : 3];

    for (const key of ['USL', 'LSL'] as LimitKey[]) {
      const data = this.limitData(points, key);
      if (!data.length) continue;
      chartSeries.push({ name: key, data });
      strokeCurves.push('stepline');
      dashes.push(5);
      widths.push(1.4);
      colors.push(COLORS.limit);
      markerSizes.push(0);
    }

    // Out-of-spec readings - red markers on the reading series
    const discrete: ApexDiscretePoint[] = [];
    points.forEach((p, index) => {
      if (p.Is_Out_Of_Spec) {
        discrete.push({
          seriesIndex: 0,
          dataPointIndex: index,
          fillColor: COLORS.outOfSpec,
          strokeColor: '#ffffff',
          size: this.interactive ? 7 : 5,
          shape: 'circle',
        });
      }
    });

    const [yMin, yMax] = this.yBounds(points);
    const maxTicks = this.interactive ? 24 : 10;

    return {
      chart: {
        type: 'line',
        height: this.height,
        animations: { enabled: false },
        fontFamily: 'inherit',
        toolbar: {
          show: this.interactive,
          tools: {
            download: false,
            selection: false,
            zoom: true,
            zoomin: true,
            zoomout: true,
            pan: true,
            reset: true,
          },
        },
        zoom: { enabled: this.interactive, type: 'x' },
        parentHeightOffset: 0,
      },
      series: chartSeries,
      colors,
      stroke: { curve: strokeCurves, width: widths, dashArray: dashes },
      markers: {
        size: markerSizes,
        strokeWidth: 1,
        strokeColors: '#ffffff',
        hover: { sizeOffset: 2 },
        discrete,
      },
      dataLabels: { enabled: false },
      legend: { show: false },
      grid: {
        borderColor: '#e2e8f0',
        strokeDashArray: 3,
        padding: { left: 8, right: 16 },
      },
      xaxis: {
        type: 'numeric',
        min: count === 1 ? 0 : 1,
        max: count === 1 ? 2 : count,
        tickAmount: count <= 1 ? 2 : Math.min(count - 1, maxTicks),
        decimalsInFloat: 0,
        labels: {
          rotate: -45,
          rotateAlways: count > 6,
          hideOverlappingLabels: true,
          style: { colors: '#64748b', fontSize: '10px' },
          formatter: (value: string) => {
            const index = Math.round(Number(value)) - 1;
            return index >= 0 && index < count ? dateLabels[index] : '';
          },
        },
        axisBorder: { color: '#cbd5e1' },
        axisTicks: { color: '#cbd5e1' },
        tooltip: { enabled: false },
      },
      yaxis: {
        min: yMin,
        max: yMax,
        tickAmount: 5,
        labels: {
          style: { colors: '#64748b', fontSize: '10px' },
          formatter: (value: number) => this.format(value),
        },
      },
      annotations: { points: this.limitLabels(count) },
      tooltip: {
        shared: false,
        intersect: true,
        custom: ({ seriesIndex, dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) =>
          seriesIndex === 0 ? this.tooltipHtml(points[dataPointIndex]) : '',
      },
    };
  }

  /** Limit value at every reading position - drawn as a stepped line. */
  private limitData(points: CrReadingPoint[], key: LimitKey): XYPoint[] {
    if (!points.some((p) => p[key] !== null && p[key] !== undefined)) return [];
    return points.map((p, i) => ({ x: i + 1, y: p[key] ?? null }));
  }

  /** Spec values written only at the start and end of each limit line. */
  private limitLabels(count: number): PointAnnotations[] {
    const stats = this.series.Stats;
    const labels: PointAnnotations[] = [];

    const add = (x: number, y: number | null | undefined, text: string, anchor: 'start' | 'end', below: boolean) => {
      if (y === null || y === undefined) return;
      labels.push({
        x,
        y,
        marker: { size: 0 },
        label: {
          text: `${text} ${this.format(y)}`,
          textAnchor: anchor,
          borderWidth: 0,
          offsetY: below ? 18 : -2,
          style: {
            background: 'transparent',
            color: COLORS.limit,
            fontSize: '10px',
            fontWeight: 600,
          },
        },
      });
    };

    add(1, stats.USL_Start, 'USL', 'start', false);
    add(1, stats.LSL_Start, 'LSL', 'start', true);
    if (count > 1) {
      add(count, stats.USL_End, 'USL', 'end', false);
      add(count, stats.LSL_End, 'LSL', 'end', true);
    }

    return labels;
  }

  private yBounds(points: CrReadingPoint[]): [number, number] {
    const values: number[] = [];
    points.forEach((p) => {
      values.push(p.Reading);
      if (p.LSL !== null && p.LSL !== undefined) values.push(p.LSL);
      if (p.USL !== null && p.USL !== undefined) values.push(p.USL);
    });

    let min = Math.min(...values);
    let max = Math.max(...values);
    const span = max - min || Math.abs(max) || 1;
    min -= span * 0.12;
    max += span * 0.12;
    return [this.round(min), this.round(max)];
  }

  private tooltipHtml(point: CrReadingPoint | undefined): string {
    if (!point) return '';

    const date = new Date(point.Audit_Date).toLocaleString(undefined, {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
    const status = point.Is_Out_Of_Spec
      ? `<span style="color:${COLORS.outOfSpec};font-weight:600">Out of spec</span>`
      : '<span style="color:#059669;font-weight:600">OK</span>';
    const individual = [point.Reading1, point.Reading2, point.Reading3]
      .map((value, i) => (value === null || value === undefined ? '' : `R${i + 1} ${this.format(value)}`))
      .filter(Boolean)
      .join(' · ');
    const averageLabel = this.series.Is_Average ? ' <span style="color:#94a3b8">(avg)</span>' : '';
    const individualLine = individual ? `<div style="color:#64748b">${individual}</div>` : '';

    return `
      <div style="padding:8px 10px;font-size:12px;line-height:1.5;min-width:150px">
        <div style="color:#64748b">${date}</div>
        <div><b style="font-size:14px;color:#0f172a">${this.format(point.Reading)}</b>${averageLabel}</div>
        ${individualLine}
        <div style="color:#64748b">LSL ${this.format(point.LSL)} · USL ${this.format(point.USL)}</div>
        <div>${status}</div>
      </div>`;
  }

  private dateLabel(value: string | Date): string {
    return new Date(value).toLocaleDateString(undefined, {
      day: '2-digit',
      month: 'short',
      year: '2-digit',
    });
  }

  private format(value: number | null | undefined): string {
    return formatValue(value);
  }

  private round(value: number): number {
    return Math.round(value * 1000) / 1000;
  }
}
