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
import { ReportChart } from './report-chart';

interface HistogramBins {
  categories: string[];
  centers: number[];
  data: number[];
  binWidth: number;
}

const HISTOGRAM_COLORS = {
  bar: '#7cb5ec',
  curve: '#1976d2',
  limit: '#d32f2f',
};

/**
 * Histogram with normal curve of one mapped item - same logic as the standard
 * histogram used in the other PQ applications:
 *  - group width tied to the tolerance (about ten groups inside USL - LSL,
 *    snapped to a round step), Sturges rule when there is no tolerance;
 *  - range stretched to cover LSL / USL, which are drawn on the nearest group;
 *  - normal curve from mean / standard deviation, scaled to the frequency axis.
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
  /** Toolbar - used in the zoom modal. */
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

  /** Latest specification of the period. */
  private get lsl(): number | null {
    return this.series.Stats?.LSL_End ?? this.series.Stats?.LSL_Start ?? null;
  }

  private get usl(): number | null {
    return this.series.Stats?.USL_End ?? this.series.Stats?.USL_Start ?? null;
  }

  private buildOptions(): ApexCharts.ApexOptions {
    const readings = this.series.Points.map((p) => Number(p.Reading));
    const lsl = this.lsl;
    const usl = this.usl;

    // the bins are stretched to cover LSL / USL as well, otherwise a
    // specification line outside the data range would have nowhere to sit
    const bins = this.generateHistogramData(readings, lsl, usl);
    const { mean, stdDev } = this.meanAndStdDev(readings);
    const curve = this.generateNormalCurve(bins.centers, mean, stdDev, readings.length, bins.binWidth);

    // vertical specification lines
    const specLines: XAxisAnnotations[] = [];
    if (lsl !== null) {
      specLines.push(this.vline(this.findClosestBin(bins.centers, bins.categories, lsl), 'LSL ' + this.round(lsl, 3)));
    }
    if (usl !== null) {
      specLines.push(this.vline(this.findClosestBin(bins.centers, bins.categories, usl), 'USL ' + this.round(usl, 3)));
    }

    return {
      series: [
        { name: 'Frequency', type: 'column', data: bins.data },
        { name: 'Normal Distribution', type: 'line', data: curve },
      ],
      chart: {
        height: this.height,
        type: 'line',
        fontFamily: 'inherit',
        animations: { enabled: false },
        toolbar: {
          show: this.interactive,
          tools: { download: false, selection: false, zoom: true, zoomin: true, zoomout: true, pan: true, reset: true },
        },
        zoom: { enabled: this.interactive },
        parentHeightOffset: 0,
      },
      plotOptions: { bar: { columnWidth: '95%' } },
      dataLabels: { enabled: false },
      stroke: { width: [1, 3], colors: ['#fff', HISTOGRAM_COLORS.curve], curve: 'smooth' },
      colors: [HISTOGRAM_COLORS.bar, HISTOGRAM_COLORS.curve],
      xaxis: {
        categories: bins.categories,
        labels: { rotate: -45, rotateAlways: true, hideOverlappingLabels: true, style: { fontSize: '9px', colors: '#64748b' } },
        // there can be up to 25 groups, so only some labels are printed
        tickAmount: bins.categories.length > 10 ? 10 : undefined,
        title: { text: 'Reading group', style: { color: '#94a3b8', fontSize: '10px', fontWeight: 500 } },
        tooltip: { enabled: false },
      },
      // the normal curve is a fraction, without this the axis prints a long tail of decimals
      yaxis: {
        min: 0,
        title: { text: 'Frequency', style: { color: '#94a3b8', fontSize: '10px', fontWeight: 500 } },
        labels: {
          style: { colors: '#64748b', fontSize: '10px' },
          formatter: (val: number) => String(this.round(val, 2)),
        },
      },
      grid: { borderColor: '#e7e7e7' },
      legend: { show: true, position: 'top', fontSize: '11px', markers: { size: 5 } },
      markers: { size: 0 },
      tooltip: {
        shared: true,
        y: { formatter: (val: number) => String(this.round(val, 2)) },
      },
      annotations: { xaxis: specLines },
    } as ApexCharts.ApexOptions;
  }

  //*************************************** Histogram logic ***************************************//

  /*  A round step near the value asked for: 1, 2, 2.5 or 5 times a
   *  power of ten. 0.05 instead of 0.043, 0.2 instead of 0.18, and so on.
   */
  private niceStep(raw: number): number {
    if (!raw || raw <= 0) {
      return 1;
    }
    const power = Math.pow(10, Math.floor(Math.log10(raw)));
    const norm = raw / power;
    let nice = 10;
    if (norm <= 1) {
      nice = 1;
    } else if (norm <= 2) {
      nice = 2;
    } else if (norm <= 2.5) {
      nice = 2.5;
    } else if (norm <= 5) {
      nice = 5;
    }
    return nice * power;
  }

  /*  Grouping for the histogram.
   *
   *  When the item has a tolerance (USL - LSL) the group width is tied to it:
   *  about ten groups inside the tolerance, snapped to a round step and
   *  started on a round value. The step is doubled while the readings are
   *  spread so wide that it would make more than 25 bars.
   *
   *  Without a tolerance it falls back to Sturges rule k = ceil(log2(n)) + 1,
   *  between 3 and 12.
   *
   *  Either way the range is stretched to cover LSL / USL, so the
   *  specification lines always have a group to sit on.
   */
  private generateHistogramData(data: number[], lsl: number | null, usl: number | null): HistogramBins {
    let min = Math.min(...data);
    let max = Math.max(...data);
    if (lsl !== null && lsl < min) {
      min = lsl;
    }
    if (usl !== null && usl > max) {
      max = usl;
    }

    const tolerance = lsl !== null && usl !== null && usl > lsl ? usl - lsl : 0;

    let binWidth: number;
    let start: number;

    if (tolerance > 0) {
      binWidth = this.niceStep(tolerance / 10);
      let guard = 0;
      while (binWidth > 0 && (max - min) / binWidth > 25 && guard < 10) {
        binWidth = this.niceStep(binWidth * 2);
        guard++;
      }
      // start on a round multiple of the step
      start = Math.floor(min / binWidth) * binWidth;
    } else {
      let k = Math.ceil(Math.log2(data.length > 1 ? data.length : 2)) + 1;
      if (k < 3) {
        k = 3;
      }
      if (k > 12) {
        k = 12;
      }
      binWidth = (max - min) / k;
      start = min;
    }

    if (!binWidth || binWidth <= 0) {
      // every reading is the same value, keep one readable group
      binWidth = 1;
      start = min - 0.5;
    }

    let binCount = Math.ceil((max - start) / binWidth + 0.000000001);
    if (binCount < 3) {
      binCount = 3;
    }

    // how many decimals the labels need, taken from the step itself
    let decimals = Math.max(0, -Math.floor(Math.log10(binWidth)));
    if (decimals > 3) {
      decimals = 3;
    }

    const bins: number[] = new Array(binCount).fill(0);
    const categories: string[] = [];
    const centers: number[] = [];
    for (let i = 0; i < binCount; i++) {
      const from = start + i * binWidth;
      const to = from + binWidth;
      categories.push(from.toFixed(decimals) + ' - ' + to.toFixed(decimals));
      centers.push(from + binWidth / 2);
    }

    data.forEach((value) => {
      let index = Math.floor((value - start) / binWidth);
      if (index < 0) {
        index = 0;
      }
      if (index > binCount - 1) {
        index = binCount - 1;
      }
      bins[index]++;
    });

    return { categories, centers, data: bins, binWidth };
  }

  // the specification line is drawn on the group whose middle is nearest to it
  private findClosestBin(centers: number[], categories: string[], value: number): string {
    if (!centers.length) {
      return '';
    }
    let best = 0;
    let minDiff = Math.abs(centers[0] - value);
    for (let i = 1; i < centers.length; i++) {
      const diff = Math.abs(centers[i] - value);
      if (diff < minDiff) {
        minDiff = diff;
        best = i;
      }
    }
    return categories[best];
  }

  // the curve is taken at the middle of every group, so it lines up with the bars
  private generateNormalCurve(
    centers: number[],
    mean: number,
    stdDev: number,
    totalCount: number,
    binWidth: number
  ): number[] {
    if (!stdDev) {
      return centers.map(() => 0);
    }
    return centers.map((x) => {
      // (1 / (sd * sqrt(2pi))) * e^(-((x - mean)^2 / (2 sd^2)))
      const coefficient = 1 / (stdDev * Math.sqrt(2 * Math.PI));
      const exponent = -Math.pow(x - mean, 2) / (2 * Math.pow(stdDev, 2));
      // scaled so that the curve sits on the same axis as the frequency
      return this.round(coefficient * Math.exp(exponent) * totalCount * binWidth, 4);
    });
  }

  /** Mean and sample standard deviation (n - 1) of the readings. */
  private meanAndStdDev(values: number[]): { mean: number; stdDev: number } {
    const n = values.length;
    if (!n) return { mean: 0, stdDev: 0 };

    const mean = values.reduce((sum, v) => sum + v, 0) / n;
    if (n < 2) return { mean, stdDev: 0 };

    const variance = values.reduce((sum, v) => sum + Math.pow(v - mean, 2), 0) / (n - 1);
    return { mean, stdDev: Math.sqrt(variance) };
  }

  private vline(category: string, text: string): XAxisAnnotations {
    return {
      x: category,
      borderColor: HISTOGRAM_COLORS.limit,
      strokeDashArray: 5,
      label: {
        text,
        orientation: 'horizontal',
        borderColor: HISTOGRAM_COLORS.limit,
        style: { background: HISTOGRAM_COLORS.limit, color: '#fff', fontSize: '10px', fontWeight: 600 },
      },
    };
  }

  private round(value: number, decimals: number): number {
    const factor = Math.pow(10, decimals);
    return Math.round(value * factor) / factor;
  }
}
