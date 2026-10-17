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
import { CrReportSeries } from './report.model';
import { CHART_COLORS, ReportChart, formatValue } from './report-chart';

interface HistogramBin {
  from: number;
  to: number;
  center: number;
  count: number;
  outOfSpec: boolean;
}

const MIN_BINS = 5;
const MAX_BINS = 20;

/**
 * Histogram of one mapped item: readings grouped into equal-width bins, with
 * LSL / USL as dashed vertical lines (latest spec of the period). Bins whose
 * centre lies outside the spec are drawn red.
 */
@Component({
  selector: 'app-cr-histogram-chart',
  standalone: true,
  template: `<div #chartHost class="w-full"></div>`,
})
export class HistogramChartComponent implements ReportChart, AfterViewInit, OnChanges, OnDestroy {
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

  private get lsl(): number | null {
    return this.series.Stats?.LSL_End ?? this.series.Stats?.LSL_Start ?? null;
  }

  private get usl(): number | null {
    return this.series.Stats?.USL_End ?? this.series.Stats?.USL_Start ?? null;
  }

  private buildOptions(): ApexCharts.ApexOptions {
    const values = this.series.Points.map((p) => Number(p.Reading));
    const { bins, min, max } = this.buildBins(values);
    const total = values.length;
    // Headroom so the count label of the tallest bar stays inside the chart
    const yMax = Math.max(1, Math.ceil(Math.max(...bins.map((b) => b.count)) * 1.15));

    return {
      chart: {
        type: 'bar',
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
      series: [
        {
          name: 'Readings',
          data: bins.map((bin) => ({
            x: bin.center,
            y: bin.count,
            fillColor: bin.outOfSpec ? CHART_COLORS.outOfSpec : CHART_COLORS.reading,
          })),
        },
      ],
      plotOptions: {
        bar: { columnWidth: '96%', borderRadius: 2, dataLabels: { position: 'top' } },
      },
      dataLabels: {
        enabled: bins.length <= 12,
        formatter: (value: number) => (value ? String(value) : ''),
        offsetY: -16,
        style: { fontSize: '10px', colors: ['#475569'], fontWeight: 500 },
      },
      legend: { show: false },
      grid: {
        borderColor: '#e2e8f0',
        strokeDashArray: 3,
        padding: { left: 8, right: 16 },
      },
      xaxis: {
        type: 'numeric',
        min,
        max,
        tickAmount: Math.min(bins.length, this.interactive ? 16 : 8),
        labels: {
          rotate: -45,
          hideOverlappingLabels: true,
          style: { colors: '#64748b', fontSize: '10px' },
          formatter: (value: string) => formatValue(Number(value)),
        },
        axisBorder: { color: '#cbd5e1' },
        axisTicks: { color: '#cbd5e1' },
        tooltip: { enabled: false },
      },
      yaxis: {
        min: 0,
        max: yMax,
        forceNiceScale: true,
        labels: {
          style: { colors: '#64748b', fontSize: '10px' },
          formatter: (value: number) => String(Math.round(value)),
        },
        title: { text: 'Count', style: { color: '#94a3b8', fontSize: '10px', fontWeight: 500 } },
      },
      annotations: { xaxis: this.limitLines() },
      tooltip: {
        custom: ({ dataPointIndex }: { dataPointIndex: number }) => {
          const bin = bins[dataPointIndex];
          if (!bin) return '';
          const percent = total ? ((bin.count / total) * 100).toFixed(1) : '0';
          const status = bin.outOfSpec
            ? `<div style="color:${CHART_COLORS.outOfSpec};font-weight:600">Out of spec</div>`
            : '';
          return `
            <div style="padding:8px 10px;font-size:12px;line-height:1.5;min-width:140px">
              <div style="color:#64748b">${formatValue(bin.from)} – ${formatValue(bin.to)}</div>
              <div><b style="font-size:14px;color:#0f172a">${bin.count}</b> readings
                <span style="color:#94a3b8">(${percent}%)</span></div>
              ${status}
            </div>`;
        },
      },
      states: { hover: { filter: { type: 'darken' } } },
    } as ApexCharts.ApexOptions;
  }

  /**
   * Equal-width bins over the readings, widened to include LSL / USL so the
   * spec lines are always visible. Bin count ~ sqrt(n), between 5 and 20.
   */
  private buildBins(values: number[]): { bins: HistogramBin[]; min: number; max: number; width: number } {
    const lsl = this.lsl;
    const usl = this.usl;

    let min = Math.min(...values, ...(lsl !== null ? [lsl] : []));
    let max = Math.max(...values, ...(usl !== null ? [usl] : []));
    if (min === max) {
      min -= 0.5;
      max += 0.5;
    }

    const count = Math.min(MAX_BINS, Math.max(MIN_BINS, Math.ceil(Math.sqrt(values.length))));
    // Small margin so the extreme bins and spec lines don't touch the axis edge
    const margin = (max - min) * 0.04;
    min -= margin;
    max += margin;
    const width = (max - min) / count;

    const bins: HistogramBin[] = Array.from({ length: count }, (_, i) => {
      const from = min + i * width;
      const to = from + width;
      const center = from + width / 2;
      return {
        from,
        to,
        center,
        count: 0,
        outOfSpec: (lsl !== null && center < lsl) || (usl !== null && center > usl),
      };
    });

    values.forEach((value) => {
      const index = Math.min(count - 1, Math.max(0, Math.floor((value - min) / width)));
      bins[index].count++;
    });

    return { bins, min, max, width };
  }

  private limitLines(): XAxisAnnotations[] {
    const lines: XAxisAnnotations[] = [];

    const add = (value: number | null, text: string) => {
      if (value === null) return;
      lines.push({
        x: value,
        borderColor: CHART_COLORS.limit,
        strokeDashArray: 5,
        label: {
          text: `${text} ${formatValue(value)}`,
          orientation: 'horizontal',
          borderWidth: 0,
          offsetY: -4,
          style: {
            background: 'transparent',
            color: CHART_COLORS.limit,
            fontSize: '10px',
            fontWeight: 600,
          },
        },
      });
    };

    add(this.lsl, 'LSL');
    add(this.usl, 'USL');
    return lines;
  }
}
