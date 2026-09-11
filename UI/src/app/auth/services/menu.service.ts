import { Injectable, signal } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiRequestService } from '../../services/api-request.service';
import {
  MenuItem,
  MenuResponse,
  MenuItemForComponent,
} from '../models/menu.model';
import { AppConfig } from '../../config/app.config.service';

@Injectable({
  providedIn: 'root',
})
export class MenuService {
  public pageTitle = new BehaviorSubject<string>('LSP');
  public pageTitle$ = this.pageTitle.asObservable();

  constructor(
    private apiService: ApiRequestService,
    private appConfig: AppConfig,
  ) {}

  /**
   * Load user menu based on token, plant ID, and audit type
   */

  getUserMenu(
    tokenNo: string,
    Plant_ID: string,
    auditType: string,
  ): Observable<MenuResponse> {
    const apiUrl = `MM_EmployeeMaster/UserAuthentication/${tokenNo},${Plant_ID},${auditType}`;

    return this.apiService.get(apiUrl).pipe(
      map((response) => {
        if (response && Array.isArray(response) && response.length > 0) {
          return { success: true, data: response };
        } else if (
          response &&
          Array.isArray(response) &&
          response.length === 0
        ) {
          return {
            success: true,
            data: [],
            message: 'No menu access assigned',
          };
        } else {
          return { success: false, data: [], message: 'No menu data received' };
        }
      }),
      catchError((error) => {
        console.error('Error loading user menu:', error);
        return [{ success: false, data: [], message: 'Failed to load menu' }];
      }),
    );
  }

  /**
   * Transform API menu data to component-friendly format
   */
  transformMenuData(apiMenuItems: MenuItem[]): MenuItemForComponent[] {
    // Menus for user
    const data = apiMenuItems.map((a) => {
      return {
        Menu_ID: a.Menu_ID,
        ActionNames: a.SubMenuList.map((b) => b.ActionName),
      };
    });

    localStorage.setItem('MenusList', JSON.stringify(data));
    return apiMenuItems
      .sort((a, b) => a.Sort_Order - b.Sort_Order) // Sort by Sort_Order
      .map((item) => ({
        id: item.Role_Name.toLowerCase().replace(/\s+/g, ''), // Create ID from role name
        label: item.Role_Name,
        icon: this.getIconForMenu(item.Role_Name), // Get appropriate icon
        isCreate: item.Is_Create,
        isEdit: item.Is_Edit,
        isDelete: item.Is_Delete,
        menuId: item.Menu_ID,
        submenu: item.SubMenuList.sort((a, b) => a.Sort_Order - b.Sort_Order) // Sort submenus by Sort_Order
          .map((subItem) => ({
            label: subItem.LinkName,
            route: '/' + subItem.ActionName.replace(/\//g, '/'), // Convert ActionName to route
          })),
      }));
  }

  /**
   * Get appropriate icon for menu based on menu name
   */
  private getIconForMenu(menuName: string): string {
    const iconMap: { [key: string]: string } = {
      usermanagement: '👥',
      dashboard: '📊',
      analytics: '📈',
      reports: '📄',
      auditsheet: '🔍',
      quality: '✅',
      production: '🏭',
      masterconfig: '📋',
      security: '🔒',
    };

    const normalizedName = menuName.toLowerCase().replace(/\s+/g, '');
    return iconMap[normalizedName] || '📁'; // Default icon
  }

  /**
   * Store the original menu data in localStorage for permission checks
   * @param menuItems The original menu items from API
   */
  storeMenuPermissions(menuItems: MenuItem[]): void {
    if (menuItems && menuItems.length > 0) {
      // Create a simplified map of menu permissions for easier access
      const permissionsMap = menuItems.reduce(
        (acc, menu) => {
          acc[menu.Menu_ID] = {
            isCreate: menu.Is_Create,
            isEdit: menu.Is_Edit,
            isDelete: menu.Is_Delete,
          };
          return acc;
        },
        {} as Record<
          number,
          { isCreate: boolean; isEdit: boolean; isDelete: boolean }
        >,
      );

      localStorage.setItem('menuPermissions', JSON.stringify(permissionsMap));
    }
  }

  /**
   * Check if user has create permission for a specific menu
   * @param menuId The menu ID to check permissions for
   */
  canCreate(menuId: number): boolean {
    return this.checkPermission(menuId, 'isCreate');
  }

  /**
   * Check if user has edit permission for a specific menu
   * @param menuId The menu ID to check permissions for
   */
  canEdit(menuId: number): boolean {
    return this.checkPermission(menuId, 'isEdit');
  }

  /**
   * Check if user has delete permission for a specific menu
   * @param menuId The menu ID to check permissions for
   */
  canDelete(menuId: number): boolean {
    return this.checkPermission(menuId, 'isDelete');
  }

  // GetMenuIDByActionName
  /**
   * Get Menu_ID by ActionName from stored menu data
   * @param actionName The ActionName to search for
   * @returns Menu_ID if found, otherwise null
   */
  // getMenuIDByActionName(actionUrl: string): number | null {
  //   const menusStr = localStorage.getItem('MenusList');
  //   if (!menusStr) return null;

  //   try {
  //     const menus = JSON.parse(menusStr);
  //     const url = new URL(actionUrl);
  //     let actionPath = url.pathname.slice(1).toLowerCase();
  //     debugger;
  //     for (const menu of menus) {
  //       if (
  //         menu.ActionNames &&
  //         menu.ActionNames.some((sub: any) => sub?.toLowerCase().trim() === actionPath.toLowerCase().trim())
  //       ) {
  //         return menu.Menu_ID;
  //       }
  //     }
  //   } catch (error) {
  //     console.error('Failed to parse MenusList or URL:', error);
  //   }

  //   return null; // Not found
  // }

  getMenuIDByActionName(actionUrl: string): number | null {
    const menusStr = localStorage.getItem('MenusList');
    if (!menusStr) return null;
    debugger;
    try {
      const menus = JSON.parse(menusStr);
      const url = new URL(actionUrl);
      let actionPath = url.pathname.toLowerCase();

      // Remove known prefix if present (e.g., /pqlsp/)
      let knownPrefix = '/pqlsp/';
      if (this.appConfig.apiPort == '443') {
        knownPrefix = '/pqlsp/portal/';
      }
      if (actionPath.startsWith(knownPrefix)) {
        actionPath = actionPath.slice(knownPrefix.length);
      } else if (actionPath.startsWith('/')) {
        actionPath = actionPath.slice(1); // Remove leading slash
      }

      for (const menu of menus) {
        if (
          menu.ActionNames &&
          menu.ActionNames.some(
            (sub: any) => sub?.toLowerCase().trim() === actionPath.trim(),
          )
        ) {
          return menu.Menu_ID;
        }
      }
    } catch (error) {
      console.error('Failed to parse MenusList or URL:', error);
    }

    return null; // Not found
  }

  /**
   * Generic method to check a specific permission for a menu
   * @param menuId The menu ID to check
   * @param permissionType The type of permission to check (isCreate, isEdit, isDelete)
   */
  private checkPermission(
    menuId: number,
    permissionType: 'isCreate' | 'isEdit' | 'isDelete',
  ): boolean {
    const permissionsStr = localStorage.getItem('menuPermissions');
    if (!permissionsStr) return false;

    try {
      const permissions = JSON.parse(permissionsStr) as Record<
        number,
        { isCreate: boolean; isEdit: boolean; isDelete: boolean }
      >;
      return permissions[menuId]?.[permissionType] || false;
    } catch (err) {
      console.error('Error parsing menu permissions:', err);
      return false;
    }
  }

  /**
   * Load menu for current authenticated user
   */
  loadCurrentUserMenu(): Observable<MenuItemForComponent[]> {
    const token = localStorage.getItem('user');
    const Plant_ID = localStorage.getItem('Plant_ID');
    const auditType = localStorage.getItem('Audit_Type_Id');

    if (!token || !Plant_ID || !auditType) {
      console.error('Missing authentication data for menu loading');
      return new Observable((observer) => {
        observer.next([]);
        observer.complete();
      });
    }

    return this.getUserMenu(token, Plant_ID, auditType).pipe(
      map((response) => {
        if (response.success) {
          this.storeMenuPermissions(response.data); // Store permissions for later checks
          return this.transformMenuData(response.data);
        } else {
          console.error('Failed to load menu:', response.message);
          return [];
        }
      }),
    );
  }
}
