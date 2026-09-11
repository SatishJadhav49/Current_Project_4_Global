import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-no-access',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div class="max-w-md w-full bg-white rounded-lg shadow-md p-8 text-center">
        <div class="mb-6">
          <div class="mx-auto w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mb-4">
            <svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
            </svg>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 mb-2">Access Denied</h1>
          <p class="text-gray-600 mb-6">
            You don't have permission to access this application. Please contact your administrator.
          </p>
        </div>
        
        <div class="space-y-4">
          <button 
            (click)="retryAuthentication()"
            class="w-full bg-blue-600 text-white py-2 px-4 rounded-md  transition-colors">
            Try Again
          </button>
          
          <button 
            (click)="contactSupport()"
            class="w-full bg-gray-200 text-gray-800 py-2 px-4 rounded-md hover:bg-gray-300 transition-colors">
            Contact Support
          </button>
        </div>
        
        <div class="mt-8 pt-6 border-t border-gray-200">
          <p class="text-xs text-gray-500">
            If you believe this is an error, please contact your system administrator.
          </p>
        </div>
      </div>
    </div>
  `
})
export class NoAccessComponent {
  constructor(private router: Router) {}

  retryAuthentication(): void {
    // Navigate back to auth check
    this.router.navigate(['/']);
  }

  contactSupport(): void {
    // Here you can implement contact support functionality
    // For now, we'll show an alert
    alert('Please contact your system administrator for access.');
  }
}
