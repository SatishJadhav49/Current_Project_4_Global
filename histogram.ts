  // ********************************** Histogram Section Start *******************************//
  buildHistogram() {
    if (!this.readings.length) {
      this.histogramOptions = {};
      return;
    }
    // the bins are stretched to cover LSL / USL as well , otherwise a
    // specification line outside the data range would have nowhere to sit
    const bins = this.generateHistogramData(this.readings, this.LSL, this.USL);

    const curve = this.generateNormalCurve(
      bins.centers,
      this.stats.mean,
      this.stats.stdDev,
      this.readings.length,
      bins.binWidth
    );

    // vertical specification lines
    const annotations: any = { xaxis: [] };
    if (this.LSL !== null) {
      annotations.xaxis.push(
        this.vline(
          this.findClosestBin(bins.centers, bins.categories, this.LSL),
          '#d32f2f',
          'LSL ' + this.LSL
        )
      );
    }
    if (this.USL !== null) {
      annotations.xaxis.push(
        this.vline(
          this.findClosestBin(bins.centers, bins.categories, this.USL),
          '#d32f2f',
          'USL ' + this.USL
        )
      );
    }

    this.histogramOptions = {
      series: [
        { name: 'Frequency', type: 'column', data: bins.data },
        { name: 'Normal Distribution', type: 'line', data: curve },
      ],
      chart: {
        height: 330,
        type: 'line',
        toolbar: { show: true },
        animations: { enabled: false },
      },
      plotOptions: { bar: { columnWidth: '95%' } },
      dataLabels: { enabled: false },
      stroke: { width: [1, 3], colors: ['#fff', '#1976d2'], curve: 'smooth' },
      colors: ['#7cb5ec', '#1976d2'],
      xaxis: {
        categories: bins.categories,
        labels: { rotate: -45, style: { fontSize: '9px' } },
        // there can be up to 25 groups , so only some labels are printed
        tickAmount: bins.categories.length > 10 ? 10 : undefined,
        title: { text: 'Reading group' },
      },
      // the normal curve is a fraction , without this the axis prints
      // a long tail of decimals
      yaxis: {
        title: { text: 'Frequency' },
        labels: { formatter: (val) => this.round(val, 2) },
      },
      grid: { borderColor: '#e7e7e7' },
      legend: { show: true, position: 'top' },
      markers: { size: 0 },
      tooltip: { shared: true },
      annotations: annotations,
      title: { text: 'Histogram with Normal Curve', align: 'center' },
    };
  }

  /*  A round step near the value asked for : 1 , 2 , 2.5 or 5 times a
   *  power of ten. 0.05 instead of 0.043 , 0.2 instead of 0.18 , and so on.
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
   *  When the location has a tolerance ( USL - LSL ) the group width is tied
   *  to it : about ten groups inside the tolerance , snapped to a round step
   *  and started on a round value. A tolerance of 0.5 gives groups of 0.05 ,
   *  so the bars are narrow and the shape of the readings is visible.
   *  The step is doubled if the readings are spread so wide that it would
   *  make more than 25 bars.
   *
   *  Without a tolerance there is nothing to tie the width to , so it falls
   *  back to Sturges rule  k = ceil( log2(n) ) + 1 , between 3 and 12.
   *
   *  Either way the range is stretched to cover LSL / USL , so the
   *  specification lines always have a group to sit on.
   */
  private generateHistogramData(
    data: number[],
    lsl: number,
    usl: number
  ): {
    categories: string[];
    centers: number[];
    data: number[];
    binWidth: number;
  } {
    let min = Math.min(...data);
    let max = Math.max(...data);
    if (lsl !== null && lsl < min) {
      min = lsl;
    }
    if (usl !== null && usl > max) {
      max = usl;
    }

    const tolerance =
      lsl !== null && usl !== null && usl > lsl ? usl - lsl : 0;

    let binWidth: number;
    let start: number;

    if (tolerance > 0) {
      binWidth = this.niceStep(tolerance / 10);
      let guard = 0;
      while (
        binWidth > 0 &&
        (max - min) / binWidth > 25 &&
        guard < 10
      ) {
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
      // every reading is the same value , keep one readable group
      binWidth = 1;
      start = min - 0.5;
    }

    let binCount = Math.ceil((max - start) / binWidth + 0.000000001);
    if (binCount < 3) {
      binCount = 3;
    }

    // how many decimals the labels need , taken from the step itself
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
  private findClosestBin(
    centers: number[],
    categories: string[],
    value: number
  ): string {
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

  // the curve is taken at the middle of every group , so it lines up with the bars
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
      return coefficient * Math.exp(exponent) * totalCount * binWidth;
    });
  }
  // ********************************** Histogram Section End *******************************//
