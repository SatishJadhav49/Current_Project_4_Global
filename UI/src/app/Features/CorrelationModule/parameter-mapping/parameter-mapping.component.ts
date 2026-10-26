import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CorrelationService } from '../correlation.service';
import {
  CrAppOrder,
  CrApplication,
  CrApplicationColumn,
  CrMapping,
  CrMasterRow,
  CrParameter,
} from '../correlation.model';
import { ConfirmationService, ToastService } from '../../../services';
import { CrViewerImage, ImageViewerComponent } from '../shared/image-viewer.component';

type ViewFilter = 'all' | 'mapped' | 'unmapped';

@Component({
  selector: 'app-parameter-mapping',
  standalone: true,
  imports: [CommonModule, FormsModule, ImageViewerComponent],
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
  showColumnFilters = false;
  viewFilter: ViewFilter = 'all';
  selectedLeafKeys = new Set<string>();
  page = 1;
  readonly pageSize = 50;
  saving = false;

  readonly viewFilters: { value: ViewFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'mapped', label: 'Mapped' },
    { value: 'unmapped', label: 'Unmapped' },
  ];

  // Custom application order of this parameter (tabs here + report cards)
  appOrderOpen = false;
  appOrderLoading = false;
  appOrderSaving = false;
  appOrder: CrAppOrder[] = [];

  // Full-screen image of a row (read live from the application)
  viewerImage: CrViewerImage | null = null;

  // Mappings of this parameter - source of truth for the "mapped" state
  mappings: CrMapping[] = [];
  private mappingByKey = new Map<string, CrMapping>();
  private mappedCountByApp = new Map<number, number>();

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
      applications: this.correlationService.getApplications(this.parameterId),
      details: this.correlationService.getParameterMappings(this.parameterId),
    }).subscribe({
      next: ({ applications, details }) => {
        this.pageLoading = false;

        if (!details?.Parameter) {
          this.pageError = 'Parameter not found or it has been deleted.';
          return;
        }

        this.parameter = details.Parameter;
        this.applications = this.prepareApplications(applications);
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

  private prepareApplications(applications: CrApplication[] | null | undefined): CrApplication[] {
    return (applications ?? []).map((app) => ({
      ...app,
      Columns: [...(app.Columns ?? [])].sort((a, b) => a.Sort_Order - b.Sort_Order),
    }));
  }

  //*************************************** Application order ***************************************//

  openAppOrder(): void {
    this.appOrderOpen = true;
    this.appOrderLoading = true;
    this.appOrder = [];

    this.correlationService.getParameterAppOrder(this.parameterId).subscribe({
      next: (apps) => {
        this.appOrder = apps ?? [];
        this.appOrderLoading = false;
      },
      error: () => {
        this.appOrderLoading = false;
        this.appOrderOpen = false;
      },
    });
  }

  closeAppOrder(): void {
    if (!this.appOrderSaving) this.appOrderOpen = false;
  }

  moveApp(index: number, step: number): void {
    const target = index + step;
    if (target < 0 || target >= this.appOrder.length) return;

    const list = [...this.appOrder];
    [list[index], list[target]] = [list[target], list[index]];
    this.appOrder = list;
  }

  get hasCustomAppOrder(): boolean {
    return this.appOrder.some((app) => app.Custom_Sort_Order !== null && app.Custom_Sort_Order !== undefined);
  }

  /** Saves the shown order; reset = empty list (back to the default order). */
  saveAppOrder(reset = false): void {
    if (this.appOrderSaving) return;

    this.appOrderSaving = true;
    const appIds = reset ? [] : this.appOrder.map((app) => app.App_ID);

    this.correlationService.saveParameterAppOrder(this.parameterId, appIds).subscribe({
      next: (result) => {
        this.appOrderSaving = false;
        if (!result?.Is_Success) {
          this.toastService.showWarning('Not Saved', result?.Message ?? 'Unable to save the order.');
          return;
        }

        this.toastService.showSuccess('Saved', result.Message ?? 'Application order saved.');
        this.appOrderOpen = false;
        this.reloadApplications();
      },
      error: () => {
        this.appOrderSaving = false;
      },
    });
  }

  /** Re-reads the tabs in the new order, keeping the selected application. */
  private reloadApplications(): void {
    this.correlationService.getApplications(this.parameterId).subscribe({
      next: (applications) => {
        const selectedId = this.selectedApp?.App_ID;
        this.applications = this.prepareApplications(applications);
        this.selectedApp = this.applications.find((app) => app.App_ID === selectedId) ?? this.selectedApp;
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
    this.viewFilter = 'all';
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

  get filterableColumnCount(): number {
    return this.columns.filter((column) => column.Is_Filterable).length;
  }

  /**
   * Mapped items whose source row no longer comes back from the application
   * (deactivated / deleted at source). Shown so they can still be unmapped.
   */
  private get orphanRows(): CrMasterRow[] {
    const app = this.selectedApp;
    if (!app) return [];

    const present = new Set(this.rows.map((row) => this.leafKey(row)));
    return this.mappings
      .filter(
        (mapping) =>
          mapping.App_ID === app.App_ID &&
          !present.has(this.mappingLeafKey(mapping))
      )
      .map((mapping) => ({
        Leaf_ID: mapping.Source_Leaf_ID,
        Leaf_Type: mapping.Source_Leaf_Type ?? null,
        Display_Text: mapping.Source_Display_Text,
        Is_Orphan: true,
      }));
  }

  /** Filters the grid and keeps mapped rows on top (original order otherwise). */
  applyFilters(): void {
    const search = this.searchText.trim().toLowerCase();
    const activeFilters = Object.entries(this.columnFilters)
      .map(([key, value]) => [key, (value ?? '').trim().toLowerCase()])
      .filter(([, value]) => !!value);

    const matches = (row: CrMasterRow): boolean => {
      const mapped = this.isMapped(row);
      if (this.viewFilter === 'mapped' && !mapped) return false;
      if (this.viewFilter === 'unmapped' && mapped) return false;

      if (this.isOrphan(row)) {
        const text = this.cellValue(row, 'Display_Text').toLowerCase();
        return !activeFilters.length && (!search || text.includes(search));
      }

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
    };

    const visible = [...this.orphanRows, ...this.rows].filter(matches);
    this.filteredRows = [
      ...visible.filter((row) => this.isMapped(row)),
      ...visible.filter((row) => !this.isMapped(row)),
    ];

    if (this.page > this.totalPages) {
      this.page = this.totalPages;
    }
  }

  setViewFilter(filter: ViewFilter): void {
    if (this.viewFilter === filter) return;
    this.viewFilter = filter;
    this.page = 1;
    this.applyFilters();
  }

  onFilterChange(): void {
    this.page = 1;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchText = '';
    this.columnFilters = {};
    this.viewFilter = 'all';
    this.page = 1;
    this.applyFilters();
  }

  get hasActiveFilters(): boolean {
    return (
      !!this.searchText.trim() ||
      this.viewFilter !== 'all' ||
      Object.values(this.columnFilters).some((value) => !!value?.trim())
    );
  }

  get activeColumnFilterCount(): number {
    return Object.values(this.columnFilters).filter((value) => !!value?.trim())
      .length;
  }

  //*************************************** Row helpers ***************************************//

  cellValue(row: CrMasterRow, key: string): string {
    const value = row[key];
    return value === null || value === undefined ? '' : String(value);
  }

  /** A leaf is identified by its ID plus optional type (1D: Gap / Flush share one Location_ID). */
  leafKey(row: CrMasterRow): string {
    return this.buildLeafKey(row['Leaf_ID'], row['Leaf_Type']);
  }

  private mappingLeafKey(mapping: CrMapping): string {
    return this.buildLeafKey(mapping.Source_Leaf_ID, mapping.Source_Leaf_Type);
  }

  private buildLeafKey(leafId: unknown, leafType: unknown): string {
    const id = leafId === null || leafId === undefined ? '' : String(leafId);
    const type =
      leafType === null || leafType === undefined
        ? ''
        : String(leafType).trim().toUpperCase();
    return `${id}|${type}`;
  }

  private mappingKey(appId: number, leafKey: string): string {
    return `${appId}|${leafKey}`;
  }

  mappingOf(row: CrMasterRow): CrMapping | undefined {
    if (!this.selectedApp) return undefined;
    return this.mappingByKey.get(
      this.mappingKey(this.selectedApp.App_ID, this.leafKey(row))
    );
  }

  isMapped(row: CrMasterRow): boolean {
    return !!this.mappingOf(row);
  }

  isOrphan(row: CrMasterRow): boolean {
    return row['Is_Orphan'] === true;
  }

  otherParameters(row: CrMasterRow): string {
    return this.cellValue(row, 'Other_Parameters');
  }

  /** Section label shown above the first mapped / first available row of a page. */
  sectionLabel(index: number): string {
    const rows = this.pagedRows;
    const mapped = this.isMapped(rows[index]);

    if (index > 0 && this.isMapped(rows[index - 1]) === mapped) return '';
    return mapped ? 'Mapped' : 'Available';
  }

  get selectedAppMappedCount(): number {
    return this.selectedApp ? this.mappedCount(this.selectedApp.App_ID) : 0;
  }

  get selectedAppAvailableCount(): number {
    return this.rows.filter((row) => !this.isMapped(row)).length;
  }

  mappedCount(appId: number): number {
    return this.mappedCountByApp.get(appId) ?? 0;
  }

  //*************************************** Image ***************************************//

  /** Set by the API only when the source application has an image for the row. */
  hasImage(row: CrMasterRow): boolean {
    return row['Has_Image'] === true;
  }

  openImage(row: CrMasterRow): void {
    const app = this.selectedApp;
    if (!app) return;

    const title = this.isOrphan(row)
      ? this.cellValue(row, 'Display_Text')
      : this.cellValue(row, 'Display_Text') || this.cellValue(row, 'Leaf_Name');

    this.viewerImage = {
      src: this.correlationService.imageUrl(app.App_ID, this.cellValue(row, 'Leaf_ID')),
      title: title || this.cellValue(row, 'Leaf_ID'),
      subtitle: [app.App_Name, this.cellValue(row, 'Leaf_Type')].filter(Boolean).join(' · '),
    };
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

  /**
   * Header checkbox works on one kind of row only, so a bulk action never
   * mixes map and unmap: mapped rows in the "Mapped" view, otherwise available rows.
   */
  private get bulkSelectableRows(): CrMasterRow[] {
    const wantMapped = this.viewFilter === 'mapped';
    return this.filteredRows.filter((row) => this.isMapped(row) === wantMapped);
  }

  get allSelected(): boolean {
    const rows = this.bulkSelectableRows;
    return rows.length > 0 && rows.every((row) => this.isSelected(row));
  }

  toggleSelectAll(): void {
    const rows = this.bulkSelectableRows;
    const select = !this.allSelected;
    rows.forEach((row) =>
      select
        ? this.selectedLeafKeys.add(this.leafKey(row))
        : this.selectedLeafKeys.delete(this.leafKey(row))
    );
  }

  clearSelection(): void {
    this.selectedLeafKeys.clear();
  }

  private get selectedRows(): CrMasterRow[] {
    return [...this.orphanRows, ...this.rows].filter((row) =>
      this.selectedLeafKeys.has(this.leafKey(row))
    );
  }

  get selectedToMapCount(): number {
    return this.selectedRows.filter((row) => !this.isMapped(row)).length;
  }

  get selectedToUnmapCount(): number {
    return this.selectedRows.filter((row) => this.isMapped(row)).length;
  }

  //*************************************** Map / Unmap ***************************************//

  mapSelected(): void {
    const app = this.selectedApp;
    const rowsToMap = this.selectedRows.filter((row) => !this.isMapped(row));
    if (!app || !rowsToMap.length || this.saving) return;

    const items = rowsToMap.map((row) => ({
      Leaf_ID: Number(row['Leaf_ID']),
      Leaf_Type: this.cellValue(row, 'Leaf_Type') || null,
      Source_Model_ID:
        row['Source_Model_ID'] === null || row['Source_Model_ID'] === undefined
          ? null
          : Number(row['Source_Model_ID']),
      Display_Text:
        this.cellValue(row, 'Display_Text') || this.cellValue(row, 'Leaf_Name'),
    }));

    this.saving = true;
    this.correlationService
      .saveMappings({
        CR_Parameter_ID: this.parameterId,
        App_ID: app.App_ID,
        Items: items,
      })
      .subscribe({
        next: (result) => {
          if (!result?.Is_Success) {
            this.saving = false;
            this.toastService.showWarning(
              'Not Mapped',
              result?.Message ?? 'Unable to map the selected rows.'
            );
            return;
          }

          this.toastService.showSuccess('Mapped', result.Message ?? 'Mapped successfully.');
          rowsToMap.forEach((row) => this.selectedLeafKeys.delete(this.leafKey(row)));
          this.reloadMappings();
        },
        error: () => {
          this.saving = false;
        },
      });
  }

  unmapRow(row: CrMasterRow): void {
    const mapping = this.mappingOf(row);
    if (!mapping) return;

    this.confirmUnmap(
      [mapping],
      mapping.Source_Display_Text || this.cellValue(row, 'Leaf_Name')
    );
  }

  unmapSelected(): void {
    const targets = this.selectedRows
      .map((row) => this.mappingOf(row))
      .filter((mapping): mapping is CrMapping => !!mapping);
    if (!targets.length) return;

    this.confirmUnmap(targets, `${targets.length} selected item(s)`);
  }

  private confirmUnmap(targets: CrMapping[], itemName?: string): void {
    if (this.saving) return;

    this.confirmationService
      .confirmDelete(
        'Unmap',
        'Remove the mapping from this parameter?',
        itemName,
        'Unmap'
      )
      .then((confirmed) => {
        if (!confirmed) return;

        this.saving = true;
        this.correlationService
          .unmap({ Mapping_IDs: targets.map((mapping) => mapping.Mapping_ID) })
          .subscribe({
            next: (result) => {
              if (result?.Is_Success) {
                this.toastService.showSuccess('Unmapped', result.Message ?? 'Unmapped successfully.');
              } else {
                this.toastService.showWarning('Unmap', result?.Message ?? 'Nothing was unmapped.');
              }

              targets.forEach((mapping) =>
                this.selectedLeafKeys.delete(this.mappingLeafKey(mapping))
              );
              this.reloadMappings();
            },
            error: () => {
              this.saving = false;
            },
          });
      });
  }

  private reloadMappings(): void {
    this.correlationService.getParameterMappings(this.parameterId).subscribe({
      next: (details) => {
        this.setMappings(details?.Mappings ?? []);
        this.saving = false;
      },
      error: () => {
        this.saving = false;
      },
    });
  }

  private setMappings(mappings: CrMapping[]): void {
    this.mappings = mappings;
    this.mappingByKey = new Map(
      mappings.map((mapping) => [
        this.mappingKey(mapping.App_ID, this.mappingLeafKey(mapping)),
        mapping,
      ])
    );

    this.mappedCountByApp = new Map();
    mappings.forEach((mapping) =>
      this.mappedCountByApp.set(
        mapping.App_ID,
        (this.mappedCountByApp.get(mapping.App_ID) ?? 0) + 1
      )
    );

    this.applyFilters();
  }
}
