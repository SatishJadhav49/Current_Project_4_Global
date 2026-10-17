import { Injectable } from '@angular/core';
import { Workbook, Worksheet } from 'exceljs';
import { saveAs } from 'file-saver';
import { CrReport, CrReportSeries } from './report.model';

/** Excel (exceljs) and PDF (pdfmake) export of the correlation report. */
@Injectable({
  providedIn: 'root',
})
export class ReportExportService {
  //*************************************** Excel ***************************************//

  async exportExcel(report: CrReport, rangeText: string): Promise<void> {
    const workbook = new Workbook();
    const parameterName = report.Parameter?.Parameter_Name ?? '';

    // Summary sheet
    const summary = workbook.addWorksheet('Summary');
    summary.addRow(['Correlation Report']).font = { bold: true, size: 14 };
    summary.addRow(['Parameter', parameterName]);
    summary.addRow(['Model', report.Parameter?.Model_Name ?? '']);
    summary.addRow(['Period', rangeText]);
    summary.addRow([]);

    const summaryHeaders = [
      'Application',
      'Mapped Item',
      'Type',
      'Readings',
      'Mean',
      'Min',
      'Max',
      'Out of Spec',
      'LSL (start)',
      'USL (start)',
      'LSL (end)',
      'USL (end)',
      'Remarks',
    ];
    this.styleHeader(summary.addRow(summaryHeaders));

    report.Series.forEach((series) => {
      const s = series.Stats;
      const row = summary.addRow([
        series.App_Name ?? '',
        series.Display_Text ?? '',
        series.Source_Leaf_Type ?? '',
        s.Count,
        s.Mean ?? '',
        s.Min ?? '',
        s.Max ?? '',
        s.Out_Of_Spec_Count,
        s.LSL_Start ?? '',
        s.USL_Start ?? '',
        s.LSL_End ?? '',
        s.USL_End ?? '',
        series.Error ?? (s.Count ? '' : 'No readings in range'),
      ]);
      if (s.Out_Of_Spec_Count > 0) {
        row.getCell(8).font = { bold: true, color: { argb: 'FFDC2626' } };
      }
    });
    this.autoWidth(summary, summaryHeaders.length);

    // One sheet of readings per mapped item
    const usedNames = new Set<string>(['summary']);
    report.Series.forEach((series, index) => {
      const sheet = workbook.addWorksheet(this.sheetName(series, index, usedNames));
      sheet.addRow([series.App_Name ?? '', series.Display_Text ?? '']).font = { bold: true };
      sheet.addRow([]);

      const headers = ['Audit Date', 'Reading No', 'Reading', 'LSL', 'USL', 'Status'];
      this.styleHeader(sheet.addRow(headers));

      series.Points.forEach((point) => {
        const row = sheet.addRow([
          new Date(point.Audit_Date),
          point.Reading_No,
          point.Reading,
          point.LSL ?? '',
          point.USL ?? '',
          point.Is_Out_Of_Spec ? 'Out of spec' : 'OK',
        ]);
        row.getCell(1).numFmt = 'dd-mmm-yyyy hh:mm';
        if (point.Is_Out_Of_Spec) {
          row.font = { color: { argb: 'FFDC2626' } };
        }
      });

      if (!series.Points.length) {
        sheet.addRow([series.Error ?? 'No readings in range']);
      }
      this.autoWidth(sheet, headers.length, 4);
    });

    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(
      new Blob([buffer], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      }),
      `${this.fileBase(parameterName)}.xlsx`
    );
  }

  private styleHeader(row: ReturnType<Worksheet['addRow']>): void {
    row.eachCell((cell) => {
      cell.font = { bold: true, color: { argb: 'FFFFFFFF' } };
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF4F46E5' } };
      cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
    });
    row.height = 20;
  }

  private autoWidth(sheet: Worksheet, columns: number, fromRow = 1): void {
    for (let c = 1; c <= columns; c++) {
      let max = 10;
      sheet.getColumn(c).eachCell({ includeEmpty: false }, (cell, rowNumber) => {
        if (rowNumber < fromRow) return;
        const value = cell.value instanceof Date ? 'dd-mmm-yyyy hh:mm' : String(cell.value ?? '');
        max = Math.max(max, value.length);
      });
      sheet.getColumn(c).width = Math.min(max + 2, 60);
    }
  }

  /** Excel sheet names: max 31 chars, unique, no []:*?/\ */
  private sheetName(series: CrReportSeries, index: number, used: Set<string>): string {
    const base = `${index + 1} ${series.App_Name ?? ''} ${series.Source_Leaf_Type ?? ''}`
      .replace(/[\[\]:*?/\\]/g, ' ')
      .replace(/\s+/g, ' ')
      .trim()
      .slice(0, 31);

    let name = base || `Item ${index + 1}`;
    let suffix = 2;
    while (used.has(name.toLowerCase())) {
      name = `${base.slice(0, 28)}~${suffix++}`;
    }
    used.add(name.toLowerCase());
    return name;
  }

  //*************************************** PDF ***************************************//

  /**
   * @param images chart PNG per Mapping_ID (from the rendered X charts)
   */
  async exportPdf(report: CrReport, rangeText: string, images: Map<number, string>): Promise<void> {
    // Loaded on demand - keeps pdfmake out of the page bundle
    const pdfMakeModule: any = await import('pdfmake/build/pdfmake');
    const fontsModule: any = await import('pdfmake/build/vfs_fonts');
    const pdfMake = pdfMakeModule.default ?? pdfMakeModule;
    const fonts = fontsModule.default ?? fontsModule;
    pdfMake.vfs = fonts?.pdfMake?.vfs ?? fonts?.vfs ?? fonts;

    const parameterName = report.Parameter?.Parameter_Name ?? '';
    const content: any[] = [
      { text: 'Correlation Report', style: 'title' },
      {
        columns: [
          { text: [{ text: 'Parameter: ', style: 'muted' }, { text: parameterName, bold: true }] },
          { text: [{ text: 'Model: ', style: 'muted' }, report.Parameter?.Model_Name ?? ''] },
          { text: [{ text: 'Period: ', style: 'muted' }, rangeText], alignment: 'right' },
        ],
        margin: [0, 0, 0, 10],
      },
    ];

    report.Series.forEach((series) => {
      const spec = this.specText(series);
      const block: any[] = [
        {
          text: [
            { text: `${series.App_Name ?? ''}  `, bold: true, color: '#4f46e5' },
            { text: series.Display_Text ?? '', bold: true },
          ],
          fontSize: 10,
        },
        { text: spec, style: 'muted', color: '#e11d48', margin: [0, 2, 0, 4] },
      ];

      const image = images.get(series.Mapping_ID);
      if (image) {
        block.push({ image, width: 760 });
      } else {
        block.push({
          text: series.Error ?? 'No readings for the selected period.',
          style: 'muted',
          italics: true,
          margin: [0, 8, 0, 8],
        });
      }

      content.push({ stack: block, unbreakable: true, margin: [0, 6, 0, 12] });
    });

    const docDefinition = {
      pageSize: 'A4',
      pageOrientation: 'landscape',
      pageMargins: [36, 30, 36, 34],
      content,
      footer: (currentPage: number, pageCount: number) => ({
        text: `${parameterName} · Page ${currentPage} of ${pageCount}`,
        alignment: 'right',
        fontSize: 8,
        color: '#94a3b8',
        margin: [36, 10, 36, 0],
      }),
      styles: {
        title: { fontSize: 16, bold: true, margin: [0, 0, 0, 6] },
        muted: { fontSize: 9, color: '#475569' },
      },
      defaultStyle: { fontSize: 9, color: '#0f172a' },
    };

    pdfMake.createPdf(docDefinition).download(`${this.fileBase(parameterName)}.pdf`);
  }

  //*************************************** Helpers ***************************************//

  specText(series: CrReportSeries): string {
    const s = series.Stats;
    const part = (label: string, start?: number | null, end?: number | null) => {
      if ((start === null || start === undefined) && (end === null || end === undefined)) return '';
      return start === end || end === null || end === undefined
        ? `${label} ${this.num(start)}`
        : `${label} ${this.num(start)} → ${this.num(end)}`;
    };
    return [part('LSL', s.LSL_Start, s.LSL_End), part('USL', s.USL_Start, s.USL_End)]
      .filter(Boolean)
      .join('   ');
  }

  private num(value: number | null | undefined): string {
    if (value === null || value === undefined) return '-';
    return Number(Number(value).toFixed(3)).toString();
  }

  private fileBase(parameterName: string): string {
    const safe = parameterName.replace(/[^A-Za-z0-9-_]+/g, '_').replace(/_+/g, '_').slice(0, 60);
    return `correlation-report-${safe || 'export'}`;
  }
}
