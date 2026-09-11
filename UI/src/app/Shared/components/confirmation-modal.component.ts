import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmationService, ConfirmationConfig } from '../../services/confirmation.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-confirmation-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Modal Backdrop -->
    <div 
      *ngIf="currentConfirmation"
      class="fixed inset-0 z-[10000] overflow-y-auto"
      aria-labelledby="modal-title" 
      role="dialog" 
      aria-modal="true"
    >
      <!-- Background overlay -->
      <div class="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div 
          class="fixed inset-0 bg-gray-900 bg-opacity-75 transition-opacity duration-300 ease-out"
          [class.opacity-0]="!showModal"
          [class.opacity-100]="showModal"
          (click)="onCancel()"
          aria-hidden="true">
        </div>

        <!-- Center the modal -->
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

        <!-- Modal panel -->
        <div 
          class="inline-block align-bottom bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all duration-300 ease-out sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6"
          [class.opacity-0]="!showModal"
          [class.opacity-100]="showModal"
          [class.scale-95]="!showModal"
          [class.scale-100]="showModal"
        >
          <!-- Modal content -->
          <div class="sm:flex sm:items-start">
            <!-- Icon -->
            <div 
              class="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full sm:mx-0 sm:h-10 sm:w-10"
              [ngClass]="getIconBackgroundClass()"
            >
              <!-- Danger Icon (Delete) -->
              <svg 
                *ngIf="currentConfirmation.type === 'danger'"
                class="h-6 w-6" 
                [ngClass]="getIconColorClass()"
                fill="none" 
                viewBox="0 0 24 24" 
                stroke="currentColor" 
                stroke-width="2"
              >
                <path 
                  stroke-linecap="round" 
                  stroke-linejoin="round" 
                  d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" 
                />
              </svg>

              <!-- Warning Icon -->
              <svg 
                *ngIf="currentConfirmation.type === 'warning'"
                class="h-6 w-6" 
                [ngClass]="getIconColorClass()"
                fill="none" 
                viewBox="0 0 24 24" 
                stroke="currentColor" 
                stroke-width="2"
              >
                <path 
                  stroke-linecap="round" 
                  stroke-linejoin="round" 
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" 
                />
              </svg>

              <!-- Info Icon -->
              <svg 
                *ngIf="currentConfirmation.type === 'info'"
                class="h-6 w-6" 
                [ngClass]="getIconColorClass()"
                fill="none" 
                viewBox="0 0 24 24" 
                stroke="currentColor" 
                stroke-width="2"
              >
                <path 
                  stroke-linecap="round" 
                  stroke-linejoin="round" 
                  d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" 
                />
              </svg>
            </div>

            <!-- Content -->
            <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
              <h3 class="text-lg leading-6 font-semibold text-gray-900" id="modal-title">
                {{ currentConfirmation.title }}
              </h3>
              <div class="mt-2">
                <p class="text-sm text-gray-600">
                  {{ currentConfirmation.message }}
                </p>
                <p 
                  *ngIf="currentConfirmation.itemName" 
                  class="mt-2 text-sm font-medium text-gray-800 bg-gray-50 px-3 py-2 rounded-md border"
                >
                  <span class="text-gray-600">Item:</span> {{ currentConfirmation.itemName }}
                </p>
              </div>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="mt-5 sm:mt-4 sm:flex sm:flex-row-reverse gap-3">
            <!-- Confirm Button -->
            <button
              type="button"
              class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 text-base font-medium text-white focus:outline-none focus:ring-2 focus:ring-offset-2 sm:ml-3 sm:w-auto sm:text-sm transition-colors duration-200"
              [ngClass]="getConfirmButtonClasses()"
              (click)="onConfirm()"
            >
              {{ currentConfirmation.confirmText || 'Confirm' }}
            </button>

            <!-- Cancel Button -->
            <button
              type="button"
              class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 sm:mt-0 sm:w-auto sm:text-sm transition-colors duration-200"
              (click)="onCancel()"
            >
              {{ currentConfirmation.cancelText || 'Cancel' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
    /* Custom animation classes */
    .modal-enter {
      opacity: 0;
      transform: scale(0.95);
    }
    .modal-enter-active {
      opacity: 1;
      transform: scale(1);
      transition: opacity 300ms ease-out, transform 300ms ease-out;
    }
    .modal-leave {
      opacity: 1;
      transform: scale(1);
    }
    .modal-leave-active {
      opacity: 0;
      transform: scale(0.95);
      transition: opacity 200ms ease-in, transform 200ms ease-in;
    }
    `
  ]
})
export class ConfirmationModalComponent implements OnInit, OnDestroy {
  currentConfirmation: ConfirmationConfig | null = null;
  showModal: boolean = false;
  private subscription: Subscription = new Subscription();

  constructor(private confirmationService: ConfirmationService) {}

  ngOnInit(): void {
    this.subscription = this.confirmationService.confirmation$.subscribe(config => {
      if (config) {
        this.currentConfirmation = config;
        // Small delay to trigger animation
        setTimeout(() => {
          this.showModal = true;
        }, 10);
      } else {
        this.showModal = false;
        // Clear after animation completes
        setTimeout(() => {
          this.currentConfirmation = null;
        }, 300);
      }
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onConfirm(): void {
    if (this.currentConfirmation) {
      this.confirmationService.resolveConfirmation(this.currentConfirmation.id, true);
    }
  }

  onCancel(): void {
    if (this.currentConfirmation) {
      this.confirmationService.resolveConfirmation(this.currentConfirmation.id, false);
    }
  }

  getIconBackgroundClass(): string {
    switch (this.currentConfirmation?.type) {
      case 'danger':
        return 'bg-red-100';
      case 'warning':
        return 'bg-yellow-100';
      case 'info':
        return 'bg-blue-100';
      default:
        return 'bg-gray-100';
    }
  }

  getIconColorClass(): string {
    switch (this.currentConfirmation?.type) {
      case 'danger':
        return 'text-red-600';
      case 'warning':
        return 'text-yellow-600';
      case 'info':
        return 'text-blue-600';
      default:
        return 'text-gray-600';
    }
  }

  getConfirmButtonClasses(): string {
    const baseClasses = 'focus:ring-offset-2';
    switch (this.currentConfirmation?.type) {
      case 'danger':
        return `${baseClasses} bg-red-600 hover:bg-red-700 focus:ring-red-500`;
      case 'warning':
        return `${baseClasses} bg-yellow-600 hover:bg-yellow-700 focus:ring-yellow-500`;
      case 'info':
        return `${baseClasses} bg-blue-600 hover:bg-blue-700 focus:ring-blue-500`;
      default:
        return `${baseClasses} bg-gray-600 hover:bg-gray-700 focus:ring-gray-500`;
    }
  }
}
