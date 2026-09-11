import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsService } from '../reports.service';
import { DefectsData, VehicleInfo } from '../reports.model';
import { ToastService } from '../../../services';

@Component({
  selector: 'app-globalsearch',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './globalsearch.component.html',
  styleUrl: './globalsearch.component.css',
})
export class GlobalsearchComponent {
  searchTerm = '';
  searchedTerm = '';

  defectsData: DefectsData[] = [];
  vehicleInfo: VehicleInfo | null = null;

  searched = false;
  defectsLoading = false;
  defectsError = false;
  vehicleInfoLoading = false;
  vehicleInfoError = false;

  private readonly vehicleNumberPattern = /^[A-Za-z0-9]{8,17}$/;

  readonly reportsService = inject(ReportsService);
  readonly toastService = inject(ToastService);

  search(): void {
    const normalized = this.searchTerm.trim();

    if (!normalized) {
      this.toastService.showWarning(
        'Vehicle Number Required',
        'Please enter a vehicle / VIN number to search.'
      );
      return;
    }

    if (!this.isVehicleSearchTerm(normalized)) {
      this.toastService.showWarning(
        'Invalid Vehicle Number',
        'Vehicle / VIN number must be 8 to 17 letters or digits.'
      );
      return;
    }

    this.searchedTerm = normalized;
    this.searched = true;
    this.loadVehicleData(normalized);
  }

  reset(): void {
    this.searchTerm = '';
    this.searchedTerm = '';
    this.searched = false;
    this.vehicleInfo = null;
    this.vehicleInfoLoading = false;
    this.vehicleInfoError = false;
    this.defectsData = [];
    this.defectsLoading = false;
    this.defectsError = false;
  }

  private isVehicleSearchTerm(term: string): boolean {
    return this.vehicleNumberPattern.test(term.replace(/\s+/g, ''));
  }

  private loadVehicleData(vehicleno: string): void {
    this.vehicleInfo = null;
    this.vehicleInfoLoading = true;
    this.vehicleInfoError = false;
    this.defectsData = [];
    this.defectsLoading = true;
    this.defectsError = false;

    this.reportsService.getVehicleInfo(vehicleno).subscribe({
      next: (vehicleInfo) => {
        this.vehicleInfo = vehicleInfo ?? null;
        this.vehicleInfoLoading = false;

        const vinNumber = vehicleInfo?.VIN_Number?.trim();
        if (!vinNumber) {
          this.defectsLoading = false;
          return;
        }

        this.reportsService.getDefectsData(vinNumber).subscribe({
          next: (defects) => {
            this.defectsData = defects ?? [];
            this.defectsLoading = false;
          },
          error: (err) => {
            console.error('Defects data error', err);
            this.defectsData = [];
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

  get hasVehicleResults(): boolean {
    return this.searched && this.defectsData.length > 0;
  }

  async exportDefectsToExcel(): Promise<void> {
    if (!this.defectsData.length) return;

    const ExcelJS = await import('exceljs');
    const { saveAs } = await import('file-saver');

    const headers = [
      'Audit Type',
      'Problem Description',
      'Severity',
      'Auditor',
      'Reported Date',
      'Attribution',
      'Shop',
    ];

    const rows = this.defectsData.map((item) => [
      item.Audit_Type ?? '',
      item.Problem_Desc ?? '',
      item.Severity_Name ?? '',
      item.Auditor_Name ?? '',
      item.Reported_Date ?? '',
      item.Attribution_Name ?? '',
      item.Shop_Name ?? '',
    ]);

    const workbook = new ExcelJS.Workbook();
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
