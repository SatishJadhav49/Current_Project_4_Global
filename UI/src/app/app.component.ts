import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  RouterOutlet,
  RouterLink,
  Router,
  NavigationEnd,
} from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './auth/services/auth.service';
import { MenuService } from './auth/services/menu.service';
import { MenuItemForComponent } from './auth/models/menu.model';
import { ToastContainerComponent } from './Shared/components/toast-container.component';
import { ConfirmationModalComponent } from './Shared/components/confirmation-modal.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, ToastContainerComponent, ConfirmationModalComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  isSidebarOpen = true; // Default to open
  activeMenu = '';
  userName = 'User';
  userId = '';
  designation: string = '';
  plantname:string ='';
  isAuthenticated = false;
  showMainLayout = false;
  menuItems: MenuItemForComponent[] = [];
  isLoadingMenu = false;

  private readonly SIDEBAR_STATE_KEY = 'sidebarOpen';
  constructor(
    private router: Router,
    private authService: AuthService,
    private menuService: MenuService
  ) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        // Show main layout only for protected routes
        this.showMainLayout = this.shouldShowMainLayout(event.url);
      });
  }

  ngOnInit(): void {
    // Load saved sidebar preference
    const savedState = localStorage.getItem(this.SIDEBAR_STATE_KEY);
    this.isSidebarOpen = savedState !== null ? savedState === 'true' : true;

    // Subscribe to authentication status
    this.authService.isAuthenticated$.subscribe((isAuth) => {
      this.isAuthenticated = isAuth;
      if (isAuth) {
        this.loadUserInfo();
        this.loadUserMenu();
      } else {
        this.menuItems = []; // Clear menu items when not authenticated
      }
    });
  }

  shouldShowMainLayout(url: string): boolean {
    // Don't show main layout for auth routes and no-menu-access
    const authRoutes = ['/', '/auth', '/no-access', '/no-menu-access'];
    return !authRoutes.includes(url);
  }

  loadUserInfo(): void {
    // Get user info from localStorage through auth service
    this.userName = this.authService.getUserDisplayName();
    this.userId = this.authService.getUserId();
    this.designation = this.authService.getUserDesignation();
    this.plantname = this.authService.getPlantName();
  }

  loadUserMenu(): void {
    this.isLoadingMenu = true;
    this.menuService.loadCurrentUserMenu().subscribe({
      next: (menuItems) => {
        this.menuItems = menuItems;
        this.isLoadingMenu = false;
        console.log('Loaded menu items:', menuItems);

        // Check if user has no menu access
        if (menuItems.length === 0) {
          console.log(
            'User has no menu access, redirecting to no-menu-access page'
          );
          this.router.navigate(['/no-menu-access']);
        }
      },
      error: (error) => {
        console.error('Error loading menu:', error);
        this.isLoadingMenu = false;
        // In case of error, redirect to no-menu-access page instead of setting default menu
        this.router.navigate(['/no-menu-access']);
      },
    });
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
    // Save user preference
    localStorage.setItem(this.SIDEBAR_STATE_KEY, this.isSidebarOpen.toString());
  }

  toggleMenu(menuId: string) {
    this.activeMenu = this.activeMenu === menuId ? '' : menuId;
  }

  navigateToRoute(route: string,menuid:number) {
    localStorage.setItem("Menu_ID",menuid.toString())
    this.router.navigate([route]);
    // Close sidebar on mobile after navigation (only on small screens)
    if (window.innerWidth < 768) {
      this.isSidebarOpen = false;
      localStorage.setItem(this.SIDEBAR_STATE_KEY, 'false');
    }
  }

}
