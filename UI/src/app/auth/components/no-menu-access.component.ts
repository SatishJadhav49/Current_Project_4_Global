import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-no-menu-access',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div class="max-w-lg w-full bg-white rounded-lg shadow-md p-8 text-center">
        <div class="mb-6">
          <div class="mx-auto w-20 h-20 bg-orange-100 rounded-full flex items-center justify-center mb-4">
            <svg class="w-10 h-10 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M12 15v2m-6 0h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
            </svg>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 mb-2">No Menu Access</h1>
          <p class="text-gray-600 mb-4">
            Hello <span class="font-semibold text-gray-800">{{getUserName()}}</span>,
          </p>
          <p class="text-gray-600 mb-6">
            You don't have access to any menus in the system. Please contact your manager to request appropriate access permissions.
          </p>
        </div>
        
        <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
          <div class="flex items-center">
            <svg class="w-5 h-5 text-blue-600 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                    d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
            </svg>
            <p class="text-blue-800 text-sm">
              Your manager can assign appropriate roles and permissions to give you access to the system features.
            </p>
          </div>
        </div>
        
        <div class="space-y-3">
          <button 
            (click)="refreshAccess()"
            class="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition-colors">
            Refresh Access
          </button>
          
          <!-- <button 
            (click)="logout()"
            class="w-full bg-gray-200 text-gray-800 py-2 px-4 rounded-md hover:bg-gray-300 transition-colors">
            Logout
          </button> -->
        </div>
        
        <div class="mt-8 pt-6 border-t border-gray-200">
          <p class="text-xs text-gray-500">
            Employee ID: <span class="font-mono">{{getUserId()}}</span>
          </p>
          <p class="text-xs text-gray-400 mt-1">
            If you believe this is an error, please contact your system administrator.
          </p>
        </div>
      </div>
    </div>
  `
})
export class NoMenuAccessComponent {
  constructor(private authService: AuthService,private router:Router,private route:ActivatedRoute) {}

  getUserName(): string {
    return this.authService.getUserDisplayName();
  }

  getUserId(): string {
    return this.authService.getUserId();
  }

  refreshAccess(): void {
   this.router.navigate([''], { relativeTo: this.route });
  }

  logout(): void {
    this.authService.logout();
  }
}
