import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ConfirmationConfig {
  id: string;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'danger' | 'warning' | 'info';
  itemName?: string; // Optional item name to be deleted
}

export interface ConfirmationResult {
  id: string;
  confirmed: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ConfirmationService {
  private confirmationSubject = new BehaviorSubject<ConfirmationConfig | null>(null);
  public confirmation$ = this.confirmationSubject.asObservable();

  private resultSubject = new BehaviorSubject<ConfirmationResult | null>(null);
  public result$ = this.resultSubject.asObservable();

  constructor() { }

  /**
   * Show confirmation dialog for delete action
   * @param title - Dialog title
   * @param message - Confirmation message
   * @param itemName - Optional name of item being deleted
   * @param confirmText - Custom confirm button text (default: 'Delete')
   * @param cancelText - Custom cancel button text (default: 'Cancel')
   * @returns Promise that resolves to boolean (true if confirmed, false if cancelled)
   */
  confirmDelete(
    title: string = 'Confirm Delete',
    message: string = 'Are you sure you want to delete this item? This action cannot be undone.',
    itemName?: string,
    confirmText: string = 'Delete',
    cancelText: string = 'Cancel'
  ): Promise<boolean> {
    const id = this.generateId();
    
    const config: ConfirmationConfig = {
      id,
      title,
      message,
      confirmText,
      cancelText,
      type: 'danger',
      itemName
    };

    this.confirmationSubject.next(config);

    return new Promise<boolean>((resolve) => {
      const subscription = this.result$.subscribe(result => {
        if (result && result.id === id) {
          subscription.unsubscribe();
          resolve(result.confirmed);
          // Clear the result after resolving
          this.resultSubject.next(null);
        }
      });
    });
  }

  /**
   * Show confirmation dialog for general actions
   * @param title - Dialog title
   * @param message - Confirmation message
   * @param confirmText - Custom confirm button text (default: 'Confirm')
   * @param cancelText - Custom cancel button text (default: 'Cancel')
   * @param type - Dialog type (default: 'warning')
   * @returns Promise that resolves to boolean (true if confirmed, false if cancelled)
   */
  confirm(
    title: string,
    message: string,
    confirmText: string = 'Confirm',
    cancelText: string = 'Cancel',
    type: 'danger' | 'warning' | 'info' = 'warning'
  ): Promise<boolean> {
    const id = this.generateId();
    
    const config: ConfirmationConfig = {
      id,
      title,
      message,
      confirmText,
      cancelText,
      type
    };

    this.confirmationSubject.next(config);

    return new Promise<boolean>((resolve) => {
      const subscription = this.result$.subscribe(result => {
        if (result && result.id === id) {
          subscription.unsubscribe();
          resolve(result.confirmed);
          // Clear the result after resolving
          this.resultSubject.next(null);
        }
      });
    });
  }

  /**
   * Handle user response to confirmation dialog
   * @param id - Confirmation ID
   * @param confirmed - Whether user confirmed or cancelled
   */
  resolveConfirmation(id: string, confirmed: boolean): void {
    this.resultSubject.next({ id, confirmed });
    this.confirmationSubject.next(null); // Hide the dialog
  }

  /**
   * Close confirmation dialog without action
   */
  closeConfirmation(): void {
    const currentConfig = this.confirmationSubject.value;
    if (currentConfig) {
      this.resolveConfirmation(currentConfig.id, false);
    }
  }

  private generateId(): string {
    return 'confirm_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
  }
}
