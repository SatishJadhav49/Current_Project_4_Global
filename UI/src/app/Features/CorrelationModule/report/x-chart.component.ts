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
import { CrDateRange, CrReadingPoint, CrReportSeries } from './report.model';

type LimitKey = 'LSL' | 'USL';
type XYPoint = { x: number; y: number | null };

const COLORS = {
  reading: '#4f46e5',
  limit: '#e11d48',
  outOfSpec: '#dc2626',
};

/**
 * X Chart (individual readings) of one mapped item: reading line, stepped
 * LSL / USL lines (labelled only at start and end) and red markers for
 * out-of-spec readings. Charts share the report date range so cards line up.
 */
@Component({
  selector: 'app-cr-x-chart',
  standalone: true,
  template: `<div #chartHost class="w-full"></div>`,
})
export class XChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('chartHost', { static: true }) chartHost!: ElementRef<HTMLDivElement>;

  @Input({ required: true }) series!: CrReportSeries;
  @Input({ required: true }) range!: CrDateRange;
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
    if (this.viewReady && (changes['series'] || changes['range'] || changes['height'])) {
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
    const times = points.map((p) => this.time(p));

    const readingData: XYPoint[] = points.map((p, i) => ({ x: times[i], y: p.Reading }));
    const lslData = this.stepData(points, times, 'LSL');
    const uslData = this.stepData(points, times, 'USL');

    const chartSeries: ApexAxisChartSeries = [{ name: 'Reading', data: readingData }];
    const strokeCurves: ('straight' | 'stepline')[] = ['straight'];
    const dashes = [0];
    const widths = [1.6];
    const colors = [COLORS.reading];
    const markerSizes = [this.interactive ? 4 : 3];

    for (const [name, data] of [['USL', uslData], ['LSL', lslData]] as const) {
      if (!data.length) continue;
      chartSeries.push({ name, data });
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
        type: 'datetime',
        min: this.range.from.getTime(),
        max: this.range.to.getTime(),
        labels: {
          datetimeUTC: false,
          style: { colors: '#64748b', fontSize: '10px' },
          datetimeFormatter: { year: 'yyyy', month: "MMM 'yy", day: 'dd MMM', hour: 'HH:mm' },
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
      annotations: { points: this.limitLabels(times) },
      tooltip: {
        shared: false,
        intersect: true,
        custom: ({ seriesIndex, dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) =>
          seriesIndex === 0 ? this.tooltipHtml(points[dataPointIndex]) : '',
      },
    };
  }

  /**
   * Stepped limit line: a point where the limit changes plus one at the end,
   * so a spec change shows as a step. Null limits break the line.
   */
  private stepData(points: CrReadingPoint[], times: number[], key: LimitKey): XYPoint[] {
    if (!points.some((p) => p[key] !== null && p[key] !== undefined)) return [];

    const data: XYPoint[] = [];
    let previous: number | null | undefined;

    points.forEach((p, index) => {
      const value = p[key] ?? null;
      if (index === 0 || value !== previous) {
        data.push({ x: times[index], y: value });
        previous = value;
      }
    });

    const lastIndex = points.length - 1;
    if (data[data.length - 1].x !== times[lastIndex]) {
      data.push({ x: times[lastIndex], y: points[lastIndex][key] ?? null });
    }

    return data;
  }

  /** Spec values written only at the start and end of each limit line. */
  private limitLabels(times: number[]): PointAnnotations[] {
    const stats = this.series.Stats;
    const first = times[0];
    const last = times[times.length - 1];
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

    add(first, stats.USL_Start, 'USL', 'start', false);
    add(first, stats.LSL_Start, 'LSL', 'start', true);
    if (last !== first) {
      add(last, stats.USL_End, 'USL', 'end', false);
      add(last, stats.LSL_End, 'LSL', 'end', true);
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
    const readingNo = this.series.Points.some((p) => p.Reading_No > 1)
      ? ` <span style="color:#94a3b8">(R${point.Reading_No})</span>`
      : '';

    return `
      <div style="padding:8px 10px;font-size:12px;line-height:1.5;min-width:150px">
        <div style="color:#64748b">${date}</div>
        <div><b style="font-size:14px;color:#0f172a">${this.format(point.Reading)}</b>${readingNo}</div>
        <div style="color:#64748b">LSL ${this.format(point.LSL)} · USL ${this.format(point.USL)}</div>
        <div>${status}</div>
      </div>`;
  }

  private time(point: CrReadingPoint): number {
    return new Date(point.Audit_Date).getTime();
  }

  private format(value: number | null | undefined): string {
    if (value === null || value === undefined || isNaN(value)) return '-';
    return Number(value.toFixed(3)).toString();
  }

  private round(value: number): number {
    return Math.round(value * 1000) / 1000;
  }
}
