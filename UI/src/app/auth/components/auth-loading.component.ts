import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { RedirectService } from '../services/redirect.service';

@Component({
  selector: 'app-auth-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gray-100 flex items-center justify-center px-4">
      <div class="max-w-md w-full bg-white rounded-lg shadow-md p-8 text-center">
        <div class="mb-6">
          <div class="mx-auto w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mb-4">
            <svg class="animate-spin w-8 h-8 text-blue-600" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 mb-2">Authenticating...</h1>
          <p class="text-gray-600 mb-6">
            Please wait while we verify your access.
          </p>
        </div>
        
        <div class="space-y-2">
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-blue-600 h-2 rounded-full transition-all duration-300" 
                 [style.width.%]="progress"></div>
          </div>
          <p class="text-sm text-gray-500">{{statusMessage}}</p>
        </div>
      </div>
    </div>
  `
})
export class AuthLoadingComponent implements OnInit {
  progress = 0;
  statusMessage = 'Getting authentication token...';

  constructor(
    private authService: AuthService,
    private router: Router,
    private redirectService: RedirectService
  ) {}

  ngOnInit(): void {
    this.startAuthentication();
  }

  private startAuthentication(): void {
    // Start progress animation
    this.progress = 20;
    
    this.authService.authenticate().subscribe({
      next: (success) => {
        this.progress = 100;
        
        if (success) {
          this.statusMessage = 'Authentication successful! Redirecting...';
          setTimeout(() => {
            this.redirectAfterAuthentication();
          }, 500);
        } else {
          this.statusMessage = 'Authentication failed. Redirecting...';
          setTimeout(() => {
            this.router.navigate(['/no-access']);
          }, 500);
        }
      },
      error: (error) => {
        console.error('Authentication error:', error);
        this.statusMessage = 'Authentication failed. Redirecting...';
        this.progress = 100;
        setTimeout(() => {
          this.router.navigate(['/no-access']);
        }, 1000);
      }
    });

    // Simulate progress updates
    setTimeout(() => {
      this.progress = 50;
      this.statusMessage = 'Verifying user information...';
    }, 1000);

    setTimeout(() => {
      this.progress = 80;
      this.statusMessage = 'Loading user profile...';
    }, 2000);
  }

  private redirectAfterAuthentication(): void {
    // Check if there's a stored redirect URL
    const redirectUrl = this.redirectService.getAndClearRedirectUrl();
    
    if (redirectUrl) {
      console.log(`Redirecting to stored URL: ${redirectUrl}`);
      // Navigate to the original URL the user was trying to access
      this.router.navigateByUrl(redirectUrl);
    } else {
      // Default redirect to dashboard if no stored URL
      console.log('No stored redirect URL, navigating to dashboard');
      this.router.navigate(['/dashboard']);
    }
  }
}
