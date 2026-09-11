import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { MenuService } from '../services/menu.service';
import { RedirectService } from '../services/redirect.service';

@Injectable({
  providedIn: 'root'
})
export class MenuAccessGuard implements CanActivate {

  constructor(
    private authService: AuthService,
    private menuService: MenuService,
    private router: Router,
    private redirectService: RedirectService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    // First check if user is authenticated
    if (!this.authService.isAuthenticated()) {
      // Store the attempted URL for redirecting after authentication
      this.redirectService.setRedirectUrl(state.url);

      return this.router.createUrlTree(['/auth']);
    }

    const routePath = route.routeConfig?.path;

    // Check if user has menu access for the requested route
    return this.checkMenuAccess(routePath).pipe(
      map(hasAccess => {
        if (hasAccess) {
          return true;
        } else {
          console.log(`User does not have menu access to: ${routePath}`);
          // Redirect to no-menu-access if user doesn't have permission
          return this.router.createUrlTree(['/no-menu-access']);
        }
      }),
      catchError(() => {
        console.error(`Error checking menu access for: ${routePath}`);
        // In case of error, redirect to no-menu-access
        return of(this.router.createUrlTree(['/no-menu-access']));
      })
    );
  }

  private checkMenuAccess(routePath: string | undefined): Observable<boolean> {
    return new Observable(observer => {
      // First check if we have cached menu permissions in localStorage
      const storedPermissions = localStorage.getItem('menuPermissions');

      if (storedPermissions) {
        // If we have cached permissions, check them first
        const hasAccessFromCache = this.checkCachedMenuAccess(routePath);
        if (hasAccessFromCache !== null) {
          observer.next(hasAccessFromCache);
          observer.complete();
          return;
        }
      }

      // If no cached permissions or cache check is inconclusive, load fresh menu data
      this.menuService.loadCurrentUserMenu().subscribe({
        next: (menuItems) => {
          const hasAccess = this.hasRouteAccess(routePath, menuItems);
          observer.next(hasAccess);
          observer.complete();
        },
        error: () => {
          observer.next(false);
          observer.complete();
        }
      });
    });
  }

  private checkCachedMenuAccess(routePath: string | undefined): boolean | null {
    if (!routePath) return false;

    try {
      const storedPermissions = localStorage.getItem('menuPermissions');
      if (!storedPermissions) return null;

      const permissions = JSON.parse(storedPermissions);

      // Define route to menu ID mapping based on your system
      const routeMenuIdMapping: { [key: string]: number[] } = {
        'usermanagement': [1], // Assuming usermanagement has menu ID 1
        'lsp': [2] // Assuming LSP has menu ID 2
      };

      const allowedMenuIds = routeMenuIdMapping[routePath.toLowerCase()] || [];

      // Check if user has access to any of the menu IDs for this route
      return allowedMenuIds.some(menuId => permissions[menuId]);
    } catch (error) {
      console.error('Error checking cached menu access:', error);
      return null; // Return null to indicate we should check fresh data
    }
  }

  private hasRouteAccess(routePath: string | undefined, menuItems: any[]): boolean {
    if (!routePath || menuItems.length === 0) {
      return false;
    }

    // Define route to menu mapping
    const routeMenuMapping: { [key: string]: string[] } = {
      'usermanagement': ['usermanagement', 'user management', 'user', 'users'],
      'lsp': ['lsp', 'concern', 'manufacturing concern', 'action plan']
    };

    const possibleMenuNames = routeMenuMapping[routePath.toLowerCase()] || [];

    // Check if user has access to any menu that could match this route
    return menuItems.some(menu =>
      possibleMenuNames.some(name =>
        menu.label.toLowerCase().includes(name) ||
        menu.id.toLowerCase().includes(name)
      )
    );
  }
}
