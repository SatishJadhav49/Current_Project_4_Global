import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class RedirectService {
  private readonly REDIRECT_URL_KEY = 'redirectUrl';

  /**
   * Store the URL to redirect to after authentication
   * @param url The URL to redirect to
   */
  setRedirectUrl(url: string): void {
    if (url && url !== '/auth' && url !== '/no-access' && url !== '/no-menu-access') {
      localStorage.setItem(this.REDIRECT_URL_KEY, url);
      console.log(`Stored redirect URL: ${url}`);
    }
  }

  /**
   * Get the stored redirect URL
   * @returns The stored URL or null if none exists
   */
  getRedirectUrl(): string | null {
    return localStorage.getItem(this.REDIRECT_URL_KEY);
  }

  /**
   * Clear the stored redirect URL
   */
  clearRedirectUrl(): void {
    localStorage.removeItem(this.REDIRECT_URL_KEY);
  }

  /**
   * Get and clear the redirect URL in one operation
   * @returns The stored URL or null if none exists
   */
  getAndClearRedirectUrl(): string | null {
    const url = this.getRedirectUrl();
    this.clearRedirectUrl();
    return url;
  }

  /**
   * Check if there's a stored redirect URL
   * @returns True if there's a stored URL
   */
  hasRedirectUrl(): boolean {
    return this.getRedirectUrl() !== null;
  }
}
