import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, ToastMessage } from '../../services/toast.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Toast Container - Responsive positioning -->
    <div class="fixed top-4 right-4 z-[9999] max-w-sm w-full space-y-3 pointer-events-none sm:block hidden">
      <div
        *ngFor="let toast of toasts; trackBy: trackByToastId"
        class="w-full shadow-lg rounded-lg pointer-events-auto overflow-hidden transform transition-all duration-500 ease-in-out animate-slide-in"
        [ngClass]="getToastClasses(toast.type)"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
      >
        <div class="p-2">
          <div class="flex items-center">
            <!-- Icon -->
            <div class="flex-shrink-0">
              <svg
                class="h-6 w-6"
                [ngClass]="getIconClasses(toast.type)"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2"
              >
                <path
                  *ngIf="toast.type === 'success'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
                <path
                  *ngIf="toast.type === 'error'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"
                />
                <path
                  *ngIf="toast.type === 'warning'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"
                />
                <path
                  *ngIf="toast.type === 'info'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            
            <!-- Content -->
            <div class="ml-3 w-0 flex-1">
              <p class="text-sm font-semibold leading-5" [ngClass]="getTitleClasses(toast.type)">
                {{ toast.title }}
              </p>
              <p class="mt-1 text-sm leading-5 text-wrap break-words" [ngClass]="getMessageClasses(toast.type)">
                {{ toast.message }}
              </p>
            </div>
            
            <!-- Close Button -->
            <div class="ml-4 flex-shrink-0 flex" *ngIf="toast.showClose">
              <button
                class="inline-flex rounded-md p-1.5 focus:outline-none focus:ring-2 focus:ring-offset-2 hover:bg-black hover:bg-opacity-10 transition-colors duration-200"
                [ngClass]="getCloseButtonClasses(toast.type)"
                (click)="closeToast(toast.id)"
                aria-label="Close notification"
              >
                <span class="sr-only">Close</span>
                <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                  <path
                    fill-rule="evenodd"
                    d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                    clip-rule="evenodd"
                  />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Mobile responsive container -->
    <div class="fixed top-4 left-4 right-4 z-[9999] space-y-3 pointer-events-none sm:hidden">
      <div
        *ngFor="let toast of toasts; trackBy: trackByToastId"
        class="w-full shadow-lg rounded-lg pointer-events-auto overflow-hidden transform transition-all duration-500 ease-in-out"
        [ngClass]="getToastClasses(toast.type)"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
      >
        <div class="p-2">
          <div class="flex items-center">
            <!-- Icon -->
            <div class="flex-shrink-0">
              <svg
                class="h-5 w-5"
                [ngClass]="getIconClasses(toast.type)"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2"
              >
                <path
                  *ngIf="toast.type === 'success'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
                <path
                  *ngIf="toast.type === 'error'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"
                />
                <path
                  *ngIf="toast.type === 'warning'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"
                />
                <path
                  *ngIf="toast.type === 'info'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            
            <!-- Content -->
            <div class="ml-2 w-0 flex-1">
              <p class="text-xs font-semibold leading-4" [ngClass]="getTitleClasses(toast.type)">
                {{ toast.title }}
              </p>
              <p class="mt-1 text-xs leading-4 text-wrap break-words" [ngClass]="getMessageClasses(toast.type)">
                {{ toast.message }}
              </p>
            </div>
            
            <!-- Close Button -->
            <div class="ml-2 flex-shrink-0 flex" *ngIf="toast.showClose">
              <button
                class="inline-flex rounded-md p-1 focus:outline-none focus:ring-1 focus:ring-offset-1 hover:bg-black hover:bg-opacity-10 transition-colors duration-200"
                [ngClass]="getCloseButtonClasses(toast.type)"
                (click)="closeToast(toast.id)"
                aria-label="Close notification"
              >
                <span class="sr-only">Close</span>
                <svg class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor">
                  <path
                    fill-rule="evenodd"
                    d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                    clip-rule="evenodd"
                  />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateX(100%);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }
    .animate-slide-in {
      animation: slideIn 0.3s ease-out;
    }
    `
  ]
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  toasts: ToastMessage[] = [];
  private subscription: Subscription = new Subscription();

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.subscription = this.toastService.toasts$.subscribe(toasts => {
      this.toasts = toasts;
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  closeToast(id: string): void {
    this.toastService.removeToast(id);
  }

  trackByToastId(index: number, toast: ToastMessage): string {
    return toast.id;
  }

  getToastClasses(type: string): string {
    const baseClasses = 'ring-1 ring-black ring-opacity-5 backdrop-blur-sm';
    switch (type) {
      case 'success':
        return `${baseClasses} bg-green-50 border-l-4 border-l-green-400 border border-green-200`;
      case 'error':
        return `${baseClasses} bg-red-50 border-l-4 border-l-red-400 border border-red-200`;
      case 'warning':
        return `${baseClasses} bg-yellow-50 border-l-4 border-l-yellow-400 border border-yellow-200`;
      case 'info':
        return `${baseClasses} bg-blue-50 border-l-4 border-l-blue-400 border border-blue-200`;
      default:
        return `${baseClasses} bg-gray-50 border-l-4 border-l-gray-400 border border-gray-200`;
    }
  }

  getIconClasses(type: string): string {
    switch (type) {
      case 'success':
        return 'text-green-500';
      case 'error':
        return 'text-red-500';
      case 'warning':
        return 'text-yellow-500';
      case 'info':
        return 'text-blue-500';
      default:
        return 'text-gray-500';
    }
  }

  getTitleClasses(type: string): string {
    switch (type) {
      case 'success':
        return 'text-green-800';
      case 'error':
        return 'text-red-800';
      case 'warning':
        return 'text-yellow-800';
      case 'info':
        return 'text-blue-800';
      default:
        return 'text-gray-800';
    }
  }

  getMessageClasses(type: string): string {
    switch (type) {
      case 'success':
        return 'text-green-700';
      case 'error':
        return 'text-red-700';
      case 'warning':
        return 'text-yellow-700';
      case 'info':
        return 'text-blue-700';
      default:
        return 'text-gray-700';
    }
  }

  getCloseButtonClasses(type: string): string {
    const baseClasses = 'focus:ring-offset-2';
    switch (type) {
      case 'success':
        return `${baseClasses} text-green-400 hover:text-green-600 focus:ring-green-500`;
      case 'error':
        return `${baseClasses} text-red-400 hover:text-red-600 focus:ring-red-500`;
      case 'warning':
        return `${baseClasses} text-yellow-400 hover:text-yellow-600 focus:ring-yellow-500`;
      case 'info':
        return `${baseClasses} text-blue-400 hover:text-blue-600 focus:ring-blue-500`;
      default:
        return `${baseClasses} text-gray-400 hover:text-gray-600 focus:ring-gray-500`;
    }
  }
}
