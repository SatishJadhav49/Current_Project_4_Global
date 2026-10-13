import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CorrelationService } from '../correlation.service';
import {
  CrApplication,
  CrApplicationColumn,
  CrMapping,
  CrMappingGroup,
  CrMasterRow,
  CrParameter,
} from '../correlation.model';
import { ConfirmationService, ToastService } from '../../../services';

@Component({
  selector: 'app-parameter-mapping',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './parameter-mapping.component.html',
})
export class ParameterMappingComponent implements OnInit {
  parameterId = 0;
  parameter: CrParameter | null = null;
  pageLoading = false;
  pageError = '';

  // Applications (tabs) - fully driven by MM_CR_Application
  applications: CrApplication[] = [];
  selectedApp: CrApplication | null = null;

  // Master data of the selected application
  private readonly masterCache = new Map<number, CrMasterRow[]>();
  rows: CrMasterRow[] = [];
  filteredRows: CrMasterRow[] = [];
  rowsLoading = false;
  rowsError = '';

  // Grid filters / selection / paging
  searchText = '';
  columnFilters: Record<string, string> = {};
  showUnmappedOnly = false;
  selectedLeafKeys = new Set<string>();
  page = 1;
  readonly pageSize = 50;
  mapping = false;

  // Mapped panel
  mappings: CrMapping[] = [];
  mappedGroups: CrMappingGroup[] = [];
  mappedCountByApp = new Map<number, number>();
  selectedMappingIds = new Set<number>();
  unmapping = false;

  private readonly correlationService = inject(CorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.parameterId = Number(this.route.snapshot.paramMap.get('parameterId'));

    if (!this.parameterId) {
      this.pageError = 'Invalid parameter.';
      return;
    }

    this.loadPage();
  }

  private loadPage(): void {
    this.pageLoading = true;
    this.pageError = '';

    forkJoin({
      applications: this.correlationService.getApplications(),
      details: this.correlationService.getParameterMappings(this.parameterId),
    }).subscribe({
      next: ({ applications, details }) => {
        this.pageLoading = false;

        if (!details?.Parameter) {
          this.pageError = 'Parameter not found or it has been deleted.';
          return;
        }

        this.parameter = details.Parameter;
        this.applications = (applications ?? []).map((app) => ({
          ...app,
          Columns: [...(app.Columns ?? [])].sort(
            (a, b) => a.Sort_Order - b.Sort_Order
          ),
        }));
        this.setMappings(details.Mappings ?? []);

        if (this.applications.length) {
          this.selectApp(this.applications[0]);
        }
      },
      error: () => {
        this.pageLoading = false;
        this.pageError = 'Unable to load the parameter. Please try again.';
      },
    });
  }

  back(): void {
    this.router.navigate(['/correlation/mapping-master']);
  }

  //*************************************** Tabs / Master data ***************************************//

  selectApp(app: CrApplication): void {
    if (this.selectedApp?.App_ID === app.App_ID && this.rows.length) return;

    this.selectedApp = app;
    this.searchText = '';
    this.columnFilters = {};
    this.showUnmappedOnly = false;
    this.selectedLeafKeys.clear();
    this.page = 1;

    const cached = this.masterCache.get(app.App_ID);
    if (cached) {
      this.rows = cached;
      this.rowsLoading = false;
      this.rowsError = '';
      this.applyFilters();
      return;
    }

    this.loadMasterData(app);
  }

  refreshMasterData(): void {
    if (!this.selectedApp) return;
    this.masterCache.delete(this.selectedApp.App_ID);
    this.selectedLeafKeys.clear();
    this.loadMasterData(this.selectedApp);
  }

  private loadMasterData(app: CrApplication): void {
    this.rows = [];
    this.filteredRows = [];
    this.rowsLoading = true;
    this.rowsError = '';

    this.correlationService.getMasterData(app.App_ID, this.parameterId).subscribe({
      next: (data) => {
        const rows = data?.Rows ?? [];
        this.masterCache.set(app.App_ID, rows);

        // Ignore a late response when the user already moved to another tab
        if (this.selectedApp?.App_ID !== app.App_ID) return;

        this.rows = rows;
        this.rowsLoading = false;
        this.applyFilters();
      },
      error: () => {
        if (this.selectedApp?.App_ID !== app.App_ID) return;
        this.rowsLoading = false;
        this.rowsError = `Unable to load ${app.App_Name} data.`;
      },
    });
  }

  get columns(): CrApplicationColumn[] {
    return this.selectedApp?.Columns ?? [];
  }

  applyFilters(): void {
    const search = this.searchText.trim().toLowerCase();
    const activeFilters = Object.entries(this.columnFilters)
      .map(([key, value]) => [key, (value ?? '').trim().toLowerCase()])
      .filter(([, value]) => !!value);

    this.filteredRows = this.rows.filter((row) => {
      if (this.showUnmappedOnly && this.isMapped(row)) return false;

      for (const [key, value] of activeFilters) {
        if (!this.cellValue(row, key).toLowerCase().includes(value)) {
          return false;
        }
      }

      if (search) {
        return this.columns.some((column) =>
          this.cellValue(row, column.Column_Key).toLowerCase().includes(search)
        );
      }

      return true;
    });

    if (this.page > this.totalPages) {
      this.page = this.totalPages;
    }
  }

  clearFilters(): void {
    this.searchText = '';
    this.columnFilters = {};
    this.showUnmappedOnly = false;
    this.page = 1;
    this.applyFilters();
  }

  get hasActiveFilters(): boolean {
    return (
      !!this.searchText.trim() ||
      this.showUnmappedOnly ||
      Object.values(this.columnFilters).some((value) => !!value?.trim())
    );
  }

  cellValue(row: CrMasterRow, key: string): string {
    const value = row[key];
    return value === null || value === undefined ? '' : String(value);
  }

  leafKey(row: CrMasterRow): string {
    return this.cellValue(row, 'Leaf_ID');
  }

  isMapped(row: CrMasterRow): boolean {
    return row['Is_Mapped'] === true;
  }

  otherParameters(row: CrMasterRow): string {
    return this.cellValue(row, 'Other_Parameters');
  }

  //*************************************** Paging ***************************************//

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredRows.length / this.pageSize));
  }

  get pagedRows(): CrMasterRow[] {
    const start = (this.page - 1) * this.pageSize;
    return this.filteredRows.slice(start, start + this.pageSize);
  }

  get pageStart(): number {
    return this.filteredRows.length ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get pageEnd(): number {
    return Math.min(this.page * this.pageSize, this.filteredRows.length);
  }

  goToPage(page: number): void {
    this.page = Math.min(Math.max(1, page), this.totalPages);
  }

  //*************************************** Selection ***************************************//

  toggleRow(row: CrMasterRow): void {
    if (this.isMapped(row)) return;

    const key = this.leafKey(row);
    if (this.selectedLeafKeys.has(key)) {
      this.selectedLeafKeys.delete(key);
    } else {
      this.selectedLeafKeys.add(key);
    }
  }

  isSelected(row: CrMasterRow): boolean {
    return this.selectedLeafKeys.has(this.leafKey(row));
  }

  /** Select-all works on every filtered row that is not mapped yet. */
  private get selectableRows(): CrMasterRow[] {
    return this.filteredRows.filter((row) => !this.isMapped(row));
  }

  get allFilteredSelected(): boolean {
    const selectable = this.selectableRows;
    return (
      selectable.length > 0 &&
      selectable.every((row) => this.selectedLeafKeys.has(this.leafKey(row)))
    );
  }

  toggleSelectAll(): void {
    const selectable = this.selectableRows;

    if (this.allFilteredSelected) {
      selectable.forEach((row) => this.selectedLeafKeys.delete(this.leafKey(row)));
    } else {
      selectable.forEach((row) => this.selectedLeafKeys.add(this.leafKey(row)));
    }
  }

  clearSelection(): void {
    this.selectedLeafKeys.clear();
  }

  //*************************************** Map ***************************************//

  mapSelected(): void {
    const app = this.selectedApp;
    if (!app || !this.selectedLeafKeys.size || this.mapping) return;

    const selectedRows = this.rows.filter((row) =>
      this.selectedLeafKeys.has(this.leafKey(row))
    );

    const items = selectedRows.map((row) => ({
      Leaf_ID: Number(row['Leaf_ID']),
      Source_Model_ID:
        row['Source_Model_ID'] === null || row['Source_Model_ID'] === undefined
          ? null
          : Number(row['Source_Model_ID']),
      Display_Text:
        this.cellValue(row, 'Display_Text') || this.cellValue(row, 'Leaf_Name'),
    }));

    this.mapping = true;
    this.correlationService
      .saveMappings({
        CR_Parameter_ID: this.parameterId,
        App_ID: app.App_ID,
        Items: items,
      })
      .subscribe({
        next: (result) => {
          this.mapping = false;

          if (!result?.Is_Success) {
            this.toastService.showWarning(
              'Not Mapped',
              result?.Message ?? 'Unable to map the selected rows.'
            );
            return;
          }

          this.toastService.showSuccess('Mapped', result.Message ?? 'Mapped successfully.');

          // Flag rows locally - no need to reload the whole master data
          selectedRows.forEach((row) => (row['Is_Mapped'] = true));
          this.selectedLeafKeys.clear();
          this.applyFilters();
          this.reloadMappings();
        },
        error: () => {
          this.mapping = false;
        },
      });
  }

  //*************************************** Mapped panel / Unmap ***************************************//

  private reloadMappings(): void {
    this.correlationService.getParameterMappings(this.parameterId).subscribe({
      next: (details) => this.setMappings(details?.Mappings ?? []),
    });
  }

  private setMappings(mappings: CrMapping[]): void {
    this.mappings = mappings;
    this.selectedMappingIds.clear();

    const groups = new Map<number, CrMappingGroup>();
    mappings.forEach((mapping) => {
      let group = groups.get(mapping.App_ID);
      if (!group) {
        group = {
          appId: mapping.App_ID,
          appName: mapping.App_Name ?? '',
          leafLevelName: mapping.Leaf_Level_Name ?? '',
          mappings: [],
        };
        groups.set(mapping.App_ID, group);
      }
      group.mappings.push(mapping);
    });

    // Same order as the tabs
    const order = new Map(this.applications.map((app, index) => [app.App_ID, index]));
    this.mappedGroups = Array.from(groups.values()).sort(
      (a, b) => (order.get(a.appId) ?? 999) - (order.get(b.appId) ?? 999)
    );

    this.mappedCountByApp = new Map(
      this.mappedGroups.map((group) => [group.appId, group.mappings.length])
    );
  }

  mappedCount(appId: number): number {
    return this.mappedCountByApp.get(appId) ?? 0;
  }

  toggleMappingSelection(mappingId: number): void {
    if (this.selectedMappingIds.has(mappingId)) {
      this.selectedMappingIds.delete(mappingId);
    } else {
      this.selectedMappingIds.add(mappingId);
    }
  }

  unmapOne(mapping: CrMapping): void {
    this.confirmUnmap([mapping], mapping.Source_Display_Text);
  }

  unmapSelected(): void {
    const selected = this.mappings.filter((mapping) =>
      this.selectedMappingIds.has(mapping.Mapping_ID)
    );
    if (!selected.length) return;

    this.confirmUnmap(selected, `${selected.length} selected item(s)`);
  }

  private confirmUnmap(targets: CrMapping[], itemName?: string): void {
    if (this.unmapping) return;

    this.confirmationService
      .confirmDelete(
        'Unmap',
        'Are you sure you want to unmap from this parameter?',
        itemName,
        'Unmap'
      )
      .then((confirmed) => {
        if (!confirmed) return;

        this.unmapping = true;
        this.correlationService
          .unmap({ Mapping_IDs: targets.map((mapping) => mapping.Mapping_ID) })
          .subscribe({
            next: (result) => {
              this.unmapping = false;

              if (result?.Is_Success) {
                this.toastService.showSuccess('Unmapped', result.Message ?? 'Unmapped successfully.');
              } else {
                this.toastService.showWarning('Unmap', result?.Message ?? 'Nothing was unmapped.');
              }

              this.clearMappedFlags(targets);
              this.reloadMappings();
            },
            error: () => {
              this.unmapping = false;
            },
          });
      });
  }

  /** Makes unmapped rows selectable again in any cached master data. */
  private clearMappedFlags(targets: CrMapping[]): void {
    targets.forEach((mapping) => {
      const rows = this.masterCache.get(mapping.App_ID);
      const leafKey = String(mapping.Source_Leaf_ID);
      rows
        ?.filter((row) => this.leafKey(row) === leafKey)
        .forEach((row) => (row['Is_Mapped'] = false));
    });

    this.applyFilters();
  }
}
