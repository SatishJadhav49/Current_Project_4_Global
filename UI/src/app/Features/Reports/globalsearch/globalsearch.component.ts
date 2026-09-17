import {
  AfterViewInit,
  Component,
  ElementRef,
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

  private clearDefects(): void {
    this.defectsData = [];
    this.auditSources = [];
    this.selectedSource = null;
    this.defectsLoading = false;
    this.defectsError = false;
  }

  /**
   * Builds one row per audit occurrence - a source audited on a given date.
   * Rows coming back without a problem description mean the audit was done
   * and nothing was reported, so the source is counted with zero defects.
   */
  private buildAuditSources(defects: DefectsData[]): AuditSourceGroup[] {
    const sourceMap = new Map<string, AuditSourceGroup>();

    defects.forEach((defect) => {
      const sourceName = defect.Audit_Type?.trim() || this.unknownSourceLabel;
      const auditDate = defect.Reported_Date ?? null;
      const key = `${sourceName}|${this.dateKey(auditDate)}`;

      let source = sourceMap.get(key);
      if (!source) {
        source = {
          key,
          sourceName,
          auditDate,
          defects: [],
          defectCount: 0,
          hasDefects: false,
        };
        sourceMap.set(key, source);
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

  private dateKey(value: string | Date | null): string {
    const time = this.dateValue(value);
    return time ? time.toString() : '';
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
            this.auditSources = this.buildAuditSources(this.defectsData);
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

  get hasAuditSources(): boolean {
    return this.searched && this.auditSources.length > 0;
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
