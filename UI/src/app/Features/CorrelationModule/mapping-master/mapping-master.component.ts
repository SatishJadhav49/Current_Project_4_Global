import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CorrelationService } from '../correlation.service';
import { CrModel, CrParameter } from '../correlation.model';
import { ConfirmationService, ToastService } from '../../../services';

@Component({
  selector: 'app-mapping-master',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mapping-master.component.html',
})
export class MappingMasterComponent implements OnInit {
  @ViewChild('nameInput') nameInput?: ElementRef<HTMLInputElement>;

  models: CrModel[] = [];
  selectedModelId: number | null = null;
  modelsLoading = false;

  parameters: CrParameter[] = [];
  parametersLoading = false;
  searchText = '';

  // Add / edit form - editingId 0 means a new parameter
  formOpen = false;
  editingId = 0;
  parameterName = '';
  saving = false;

  readonly maxNameLength = 250;
  private readonly modelStorageKey = 'CR_Model_ID';

  private readonly correlationService = inject(CorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.loadModels();
  }

  get filteredParameters(): CrParameter[] {
    const search = this.searchText.trim().toLowerCase();
    if (!search) return this.parameters;

    return this.parameters.filter((parameter) =>
      (parameter.Parameter_Name ?? '').toLowerCase().includes(search)
    );
  }

  get selectedModelName(): string {
    return (
      this.models.find((model) => model.Model_ID === this.selectedModelId)
        ?.Model_Name ?? ''
    );
  }

  private loadModels(): void {
    this.modelsLoading = true;

    this.correlationService.getModels().subscribe({
      next: (models) => {
        this.models = models ?? [];
        this.modelsLoading = false;

        // Come back to the model the user was working on
        const remembered = Number(this.readStorage(this.modelStorageKey));
        if (remembered && this.models.some((m) => m.Model_ID === remembered)) {
          this.selectedModelId = remembered;
          this.loadParameters();
        }
      },
      error: () => {
        this.models = [];
        this.modelsLoading = false;
      },
    });
  }

  onModelChange(): void {
    this.closeForm();
    this.searchText = '';
    this.parameters = [];

    if (this.selectedModelId) {
      this.writeStorage(this.modelStorageKey, String(this.selectedModelId));
      this.loadParameters();
    }
  }

  loadParameters(): void {
    if (!this.selectedModelId) return;

    this.parametersLoading = true;
    this.correlationService.getParameters(this.selectedModelId).subscribe({
      next: (parameters) => {
        this.parameters = parameters ?? [];
        this.parametersLoading = false;
      },
      error: () => {
        this.parameters = [];
        this.parametersLoading = false;
      },
    });
  }

  //*************************************** Add / Edit ***************************************//

  openAdd(): void {
    if (!this.selectedModelId) {
      this.toastService.showWarning('Model Required', 'Please select a model first.');
      return;
    }

    this.editingId = 0;
    this.parameterName = '';
    this.formOpen = true;
    this.focusName();
  }

  openEdit(parameter: CrParameter): void {
    this.editingId = parameter.CR_Parameter_ID;
    this.parameterName = parameter.Parameter_Name ?? '';
    this.formOpen = true;
    this.focusName();
  }

  closeForm(): void {
    this.formOpen = false;
    this.editingId = 0;
    this.parameterName = '';
  }

  saveParameter(): void {
    const name = this.parameterName.trim();

    if (!this.selectedModelId) return;

    if (!name) {
      this.toastService.showWarning('Name Required', 'Please enter a parameter name.');
      return;
    }

    // Quick client check - the database enforces it as well
    const duplicate = this.parameters.some(
      (parameter) =>
        parameter.CR_Parameter_ID !== this.editingId &&
        (parameter.Parameter_Name ?? '').trim().toLowerCase() === name.toLowerCase()
    );
    if (duplicate) {
      this.toastService.showWarning(
        'Duplicate Parameter',
        `"${name}" already exists for this model.`
      );
      return;
    }

    this.saving = true;
    this.correlationService
      .saveParameter({
        CR_Parameter_ID: this.editingId,
        Model_ID: this.selectedModelId,
        Parameter_Name: name,
      })
      .subscribe({
        next: (result) => {
          this.saving = false;

          if (result?.Is_Success) {
            this.toastService.showSuccess('Saved', result.Message ?? 'Parameter saved.');
            this.closeForm();
            this.loadParameters();
          } else {
            this.toastService.showWarning(
              'Not Saved',
              result?.Message ?? 'Unable to save the parameter.'
            );
          }
        },
        error: () => {
          this.saving = false;
        },
      });
  }

  //*************************************** Delete / Map ***************************************//

  deleteParameter(parameter: CrParameter): void {
    const mappedNote = parameter.Total_Mapped
      ? ` Its ${parameter.Total_Mapped} mapping(s) will be removed as well.`
      : '';

    this.confirmationService
      .confirmDelete(
        'Delete Parameter',
        `Are you sure you want to delete this parameter?${mappedNote}`,
        parameter.Parameter_Name
      )
      .then((confirmed) => {
        if (!confirmed) return;

        this.correlationService
          .deleteParameter({ CR_Parameter_ID: parameter.CR_Parameter_ID })
          .subscribe({
            next: (result) => {
              if (result?.Is_Success) {
                this.toastService.showSuccess('Deleted', result.Message ?? 'Parameter deleted.');
                if (this.editingId === parameter.CR_Parameter_ID) {
                  this.closeForm();
                }
                this.loadParameters();
              } else {
                this.toastService.showWarning(
                  'Not Deleted',
                  result?.Message ?? 'Unable to delete the parameter.'
                );
              }
            },
          });
      });
  }

  openMapping(parameter: CrParameter): void {
    this.router.navigate([
      '/correlation/mapping-master',
      parameter.CR_Parameter_ID,
    ]);
  }

  private focusName(): void {
    setTimeout(() => this.nameInput?.nativeElement.focus());
  }

  private readStorage(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private writeStorage(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // storage unavailable - only a convenience
    }
  }
}
