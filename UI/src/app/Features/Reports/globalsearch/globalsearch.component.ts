import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { ReportsService } from '../reports.service';
import { AuditSourceGroup, DefectsData, VehicleInfo } from '../reports.model';
import { ToastService } from '../../../services';

@Component({
  selector: 'app-globalsearch',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './globalsearch.component.html',
  styleUrl: './globalsearch.component.css',
})
export class GlobalsearchComponent implements AfterViewInit, OnDestroy {
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  searchTerm = '';
  searchedTerm = '';

  defectsData: DefectsData[] = [];
  auditSources: AuditSourceGroup[] = [];
  selectedSource: AuditSourceGroup | null = null;
  vehicleInfo: VehicleInfo | null = null;

  // Audit category filter - '' means All
  availableCategories: string[] = [];
  selectedCategory = '';

  // AI summary
  summaryOpen = false;
  summaryLoading = false;
  summaryError = '';
  summaryHtml = '';

  searched = false;
  defectsLoading = false;
  defectsError = false;
  vehicleInfoLoading = false;
  vehicleInfoError = false;

  // Auto search fires when the entered number is one of these lengths
  private readonly searchLengths = [8, 10, 17];
  private readonly alphaNumericPattern = /^[A-Za-z0-9]+$/;
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly unknownSourceLabel = 'Unknown Source';

  private readonly severityStyles: Record<string, string> = {
    critical: 'bg-rose-100 text-rose-700 ring-1 ring-rose-200',
    major: 'bg-amber-100 text-amber-800 ring-1 ring-amber-200',
    minor: 'bg-sky-100 text-sky-700 ring-1 ring-sky-200',
  };
  private readonly defaultSeverityStyle =
    'bg-gray-100 text-gray-700 ring-1 ring-gray-200';

  readonly reportsService = inject(ReportsService);
  readonly toastService = inject(ToastService);

  ngAfterViewInit(): void {
    setTimeout(() => this.searchInput?.nativeElement.focus());
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }
  }

  /** Auto search while typing - only when the term looks complete. */
  onTermChange(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }

    const normalized = this.searchTerm.trim();

    if (
      !this.isVehicleSearchTerm(normalized) ||
      normalized === this.searchedTerm
    ) {
      return;
    }

    this.debounceTimer = setTimeout(() => {
      this.runSearch(normalized);
    }, 400);
  }

  /** Manual search - search button / enter key. */
  search(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }

    const normalized = this.searchTerm.trim();

    if (!normalized) {
      this.toastService.showWarning(
        'Vehicle Number Required',
        'Please enter a VIN / BIW number to search.'
      );
      return;
    }

    if (!this.isVehicleSearchTerm(normalized)) {
      this.toastService.showWarning(
        'Invalid Vehicle Number',
        'Enter a valid VIN / BIW number of 8, 10 or 17 characters.'
      );
      return;
    }

    this.runSearch(normalized);
  }

  reset(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }

    this.searchTerm = '';
    this.searchedTerm = '';
    this.searched = false;
    this.vehicleInfo = null;
    this.vehicleInfoLoading = false;
    this.vehicleInfoError = false;
    this.clearDefects();

    this.searchInput?.nativeElement.focus();
  }

  selectSource(source: AuditSourceGroup): void {
    this.selectedSource = source;
  }

  //*************************************** AI Summary ***************************************//

  /** Defects currently in view - respects the active category filter. */
  get visibleDefects(): DefectsData[] {
    return this.auditSources.flatMap((source) => source.defects);
  }

  openSummary(): void {
    const defects = this.visibleDefects;
    if (!defects.length) return;

    this.summaryOpen = true;
    this.loadSummary(defects);
  }

  closeSummary(): void {
    this.summaryOpen = false;
  }

  retrySummary(): void {
    this.loadSummary(this.visibleDefects);
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.summaryOpen) {
      this.closeSummary();
    }
  }

  private loadSummary(defects: DefectsData[]): void {
    this.summaryLoading = true;
    this.summaryError = '';
    this.summaryHtml = '';

    this.reportsService.getDefectsSummary(defects).subscribe({
      next: (response) => {
        const summary = response?.summary?.trim() ?? '';

        if (!summary) {
          this.summaryError = 'The AI service did not return a summary.';
        } else {
          this.summaryHtml = this.markdownToHtml(summary);
        }

        this.summaryLoading = false;
      },
      error: (err) => {
        console.error('Defect summary error', err);
        this.summaryError =
          'Unable to generate the summary right now. Please try again.';
        this.summaryLoading = false;
      },
    });
  }

  /**
   * Renders the LLM markdown response. The result is bound with [innerHTML],
   * so Angular's sanitizer strips anything unsafe while keeping the
   * formatting tags.
   */
  private markdownToHtml(markdown: string): string {
    const text = (markdown ?? '').trim();
    if (!text) return '';

    // Already HTML - hand it straight to the sanitizer
    if (/<(p|div|ul|ol|li|h[1-6]|strong|em|br|table|span)\b/i.test(text)) {
      return text;
    }

    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    const html: string[] = [];
    let listType: 'ul' | 'ol' | null = null;

    const closeList = () => {
      if (listType) {
        html.push(`</${listType}>`);
        listType = null;
      }
    };

    escaped.split(/\r?\n/).forEach((rawLine) => {
      const line = rawLine.trim();

      if (!line) {
        closeList();
        return;
      }

      if (/^(-{3,}|\*{3,}|_{3,})$/.test(line)) {
        closeList();
        html.push('<hr />');
        return;
      }

      const heading = line.match(/^(#{1,6})\s+(.*)$/);
      if (heading) {
        closeList();
        const level = Math.min(heading[1].length + 2, 6);
        html.push(
          `<h${level}>${this.inlineMarkdown(heading[2])}</h${level}>`
        );
        return;
      }

      const bullet = line.match(/^[-*•]\s+(.*)$/);
      if (bullet) {
        if (listType !== 'ul') {
          closeList();
          html.push('<ul>');
          listType = 'ul';
        }
        html.push(`<li>${this.inlineMarkdown(bullet[1])}</li>`);
        return;
      }

      const numbered = line.match(/^\d+[.)]\s+(.*)$/);
      if (numbered) {
        if (listType !== 'ol') {
          closeList();
          html.push('<ol>');
          listType = 'ol';
        }
        html.push(`<li>${this.inlineMarkdown(numbered[1])}</li>`);
        return;
      }

      closeList();
      html.push(`<p>${this.inlineMarkdown(line)}</p>`);
    });

    closeList();
    return html.join('');
  }

  private inlineMarkdown(text: string): string {
    return text
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/(^|[^*])\*(?!\s)([^*]+?)\*/g, '$1<em>$2</em>')
      .replace(/`([^`]+?)`/g, '<code>$1</code>');
  }

  private clearDefects(): void {
    this.defectsData = [];
    this.auditSources = [];
    this.selectedSource = null;
    this.availableCategories = [];
    this.selectedCategory = '';
    this.defectsLoading = false;
    this.defectsError = false;

    this.summaryOpen = false;
    this.summaryLoading = false;
    this.summaryError = '';
    this.summaryHtml = '';
  }

  selectCategory(category: string): void {
    if (this.selectedCategory === category) return;

    this.selectedCategory = category;
    this.applyCategoryFilter();
  }

  /** Categories present in the current result - driven by the data, not hardcoded. */
  private buildCategories(defects: DefectsData[]): string[] {
    const categories = new Set<string>();

    defects.forEach((defect) => {
      const category = defect.Audit_Category?.trim();
      if (category) {
        categories.add(category);
      }
    });

    return Array.from(categories).sort((a, b) => a.localeCompare(b));
  }

  private applyCategoryFilter(): void {
    const filtered = this.selectedCategory
      ? this.defectsData.filter(
          (defect) =>
            (defect.Audit_Category?.trim() ?? '').toLowerCase() ===
            this.selectedCategory.toLowerCase()
        )
      : this.defectsData;

    this.auditSources = this.buildAuditSources(filtered);

    // Keep the open source selected when it survives the filter
    const previousKey = this.selectedSource?.key;
    this.selectedSource = previousKey
      ? this.auditSources.find((source) => source.key === previousKey) ?? null
      : null;
  }

  /**
   * Builds one row per audit source, carrying the latest reported date found
   * for that source. Rows coming back without a problem description mean the
   * audit was done and nothing was reported, so the source stays at zero
   * defects.
   */
  private buildAuditSources(defects: DefectsData[]): AuditSourceGroup[] {
    const sourceMap = new Map<string, AuditSourceGroup>();

    defects.forEach((defect) => {
      const sourceName = defect.Audit_Type?.trim() || this.unknownSourceLabel;
      const key = sourceName.toLowerCase();

      let source = sourceMap.get(key);
      if (!source) {
        source = {
          key,
          sourceName,
          auditDate: null,
          defects: [],
          defectCount: 0,
          hasDefects: false,
        };
        sourceMap.set(key, source);
      }

      const reportedDate = defect.Reported_Date ?? null;
      if (this.dateValue(reportedDate) > this.dateValue(source.auditDate)) {
        source.auditDate = reportedDate;
      }

      if (defect.Problem_Desc?.trim()) {
        source.defects.push(defect);
        source.defectCount = source.defects.length;
        source.hasDefects = true;
      }
    });

    return Array.from(sourceMap.values()).sort(
      (a, b) => this.dateValue(b.auditDate) - this.dateValue(a.auditDate)
    );
  }

  /** Severity badge colours - unknown severities fall back to neutral grey. */
  severityClass(severity?: string): string {
    const key = severity?.trim().toLowerCase() ?? '';
    return this.severityStyles[key] ?? this.defaultSeverityStyle;
  }

  private dateValue(value: string | Date | null): number {
    if (!value) return 0;
    const time = new Date(value).getTime();
    return isNaN(time) ? 0 : time;
  }

  private isVehicleSearchTerm(term: string): boolean {
    const value = term.replace(/\s+/g, '');
    return (
      this.alphaNumericPattern.test(value) &&
      this.searchLengths.includes(value.length)
    );
  }

  private runSearch(vehicleno: string): void {
    this.searchedTerm = vehicleno;
    this.searched = true;
    this.loadVehicleData(vehicleno);
  }

  private loadVehicleData(vehicleno: string): void {
    this.vehicleInfo = null;
    this.vehicleInfoLoading = true;
    this.vehicleInfoError = false;
    this.clearDefects();
    this.defectsLoading = true;

    this.reportsService.getVehicleInfo(vehicleno).subscribe({
      next: (vehicleInfo) => {
        this.vehicleInfo = vehicleInfo ?? null;
        this.vehicleInfoLoading = false;

        const vinNumber = vehicleInfo?.VIN_Number?.trim() ?? '';
        const biwNo = vehicleInfo?.BIW_No?.trim() ?? '';

        if (!vinNumber && !biwNo) {
          this.defectsLoading = false;
          return;
        }

        this.reportsService.getDefectsData(vinNumber, biwNo).subscribe({
          next: (defects) => {
            this.defectsData = defects ?? [];
            this.availableCategories = this.buildCategories(this.defectsData);
            this.selectedCategory = '';
            this.applyCategoryFilter();
            this.defectsLoading = false;
          },
          error: (err) => {
            console.error('Defects data error', err);
            this.defectsData = [];
            this.auditSources = [];
            this.defectsLoading = false;
            this.defectsError = true;
          },
        });
      },
      error: (err) => {
        console.error('Vehicle info error', err);
        this.vehicleInfo = null;
        this.vehicleInfoLoading = false;
        this.vehicleInfoError = true;
        this.defectsLoading = false;
      },
    });
  }

  get hasResults(): boolean {
    return this.searched && this.defectsData.length > 0;
  }

  get totalDefectCount(): number {
    return this.auditSources.reduce(
      (total, source) => total + source.defectCount,
      0
    );
  }

  get cleanSourceCount(): number {
    return this.auditSources.filter((source) => !source.hasDefects).length;
  }

  async exportDefectsToExcel(): Promise<void> {
    if (!this.totalDefectCount) return;

    const headers = [
      'Source',
      'Audit Date',
      'Audit Category',
      'Problem Description',
      'Severity',
      'Attribution',
      'Shop',
      'Auditor',
    ];

    // Export follows the same source order shown on screen
    const rows = this.auditSources.flatMap((source) =>
      source.defects.map((item) => [
        source.sourceName,
        item.Reported_Date ?? '',
        item.Audit_Category ?? '',
        item.Problem_Desc ?? '',
        item.Severity_Name ?? '',
        item.Attribution_Name ?? '',
        item.Shop_Name ?? '',
        item.Auditor_Name ?? '',
      ])
    );

    const workbook = new Workbook();
    const worksheet = workbook.addWorksheet('Vehicle Defects');

    const headerRow = worksheet.addRow(headers);
    rows.forEach((row) => worksheet.addRow(row));

    headerRow.height = 22.5;
    headerRow.eachCell((cell) => {
      cell.fill = {
        type: 'pattern',
        pattern: 'solid',
        fgColor: { argb: 'FFFFFF00' },
      };
      cell.font = { bold: true, color: { argb: 'FF000000' } };
      cell.border = {
        top: { style: 'thin', color: { argb: 'FFB7C9D9' } },
        bottom: { style: 'thin', color: { argb: 'FFB7C9D9' } },
        left: { style: 'thin', color: { argb: 'FFB7C9D9' } },
        right: { style: 'thin', color: { argb: 'FFB7C9D9' } },
      };
      cell.alignment = {
        vertical: 'middle',
        horizontal: 'center',
        wrapText: true,
      };
    });

    worksheet.eachRow((row, rowNumber) => {
      if (rowNumber === 1) return;
      row.height = 16.5;
      row.eachCell((cell) => {
        cell.alignment = { vertical: 'middle', horizontal: 'center' };
      });
    });

    headers.forEach((header, index) => {
      const lengths = rows.map((row) => String(row[index] ?? '').length);
      const maxLength = Math.max(header.length, ...lengths, 12);
      worksheet.getColumn(index + 1).width = Math.min(maxLength + 2, 50);
    });

    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });

    saveAs(blob, `vehicle-defects-${this.searchedTerm || 'export'}.xlsx`);
  }
}
