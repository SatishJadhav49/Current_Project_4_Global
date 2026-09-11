import { Injectable, signal } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import {
  TokenResponse,
  UserSearchResponse,
  AuthUser,
} from '../models/auth.model';
import { ApiRequestService } from '../../services/api-request.service';
import { RedirectService } from './redirect.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(
    private apiService: ApiRequestService,
    private redirectService: RedirectService
  ) {
    this.checkAuthStatus();
  }

  getCurrentToken(): Observable<TokenResponse> {
    return this.apiService.get('MM_EmployeeMaster/GetTokenCurrent').pipe(
      map((response) => {
        if (response) {
          return { success: true, token: response };
        } else {
          return { success: false, message: 'No token received' };
        }
      }),
      catchError((error) => {
        console.error('Error getting current token:', error);
        return [{ success: false, message: 'Failed to get token' }];
      })
    );
  }

  searchUserByToken(token: string): Observable<UserSearchResponse> {
    return this.apiService.get(`MM_EmployeeMaster/SearchToken/${token}`).pipe(
      map((response) => {
        if (response && response !== null) {
          return { success: true, data: response };
        } else {
          return { success: false, data: [], message: 'No user data found' };
        }
      }),
      catchError((error) => {
        console.error('Error searching user by token:', error);
        return [
          {
            success: false,
            data: [],
            message: 'Failed to get user information',
          },
        ];
      })
    );
  }

  authenticate(): Observable<boolean> {
    return new Observable((observer) => {
      this.getCurrentToken().subscribe({
        next: (tokenResponse) => {
          if (tokenResponse.success && tokenResponse.token) {
            this.searchUserByToken(tokenResponse.token).subscribe({
              next: (userResponse) => {
                if (userResponse.success && userResponse.data) {
                  debugger;
                  this.storeUserData(tokenResponse.token!, userResponse.data);
                  this.isAuthenticatedSubject.next(true);
                  observer.next(true);
                  observer.complete();
                } else {
                  observer.next(false);
                  observer.complete();
                }
              },
              error: (error) => {
                console.error('Authentication failed at step 2:', error);
                observer.next(false);
                observer.complete();
              },
            });
          } else {
            observer.next(false);
            observer.complete();
          }
        },
        error: (error) => {
          console.error('Authentication failed at step 1:', error);
          observer.next(false);
          observer.complete();
        },
      });
    });
  }

  private storeUserData(token: string, userData: any): void {
    localStorage.clear();
    localStorage.setItem('user', token);
    localStorage.setItem('Audit_Type_Id', userData.Audit_Type_Id);
    localStorage.setItem('Plant_ID', userData.Plant_ID);
    localStorage.setItem('Plant_Code', userData.Plant_Code);
    localStorage.setItem('Employee_ID', userData.Employee_ID);
    localStorage.setItem('Manager_ID', userData.Reporting_Manager_ID);
    localStorage.setItem('Email', userData.Employee_No + '@mahindra.com');
    localStorage.setItem('Name', userData.Employee_Name);
    localStorage.setItem('Employee_No', userData.Employee_No);

    this.apiService.get('MM_EmployeeMaster/getCurrentHostName').subscribe(
      (hostName) => {
        localStorage.setItem('Hostname', hostName);
      },
      (error) => {
        console.error('Error fetching hostname:', error);
      }
    );
  }

  getCurrentUser(): AuthUser | null {
    const token = localStorage.getItem('user');
    if (!token) return null;

    return {
      token: token,
      userType: localStorage.getItem('userType') || '',
      isAllShops: localStorage.getItem('isallshops') || '0',
      Audit_Type_Id: localStorage.getItem('Audit_Type_Id') || '',
      Plant_ID: localStorage.getItem('Plant_ID') || '',
      plantCode: localStorage.getItem('Plant_Code') || '',
      userId: localStorage.getItem('userid') || '',
      managerId: localStorage.getItem('Manager_ID') || '',
      email: localStorage.getItem('Email') || '',
      name: localStorage.getItem('Name') || '',
      shopId: localStorage.getItem('shopid') || '',
    };
  }

  isAuthenticated(): boolean {
    const user = this.getCurrentUser();
    return user !== null && user.token !== '';
  }

  private checkAuthStatus(): void {
    const isAuth = this.isAuthenticated();
    this.isAuthenticatedSubject.next(isAuth);
  }

  logout(): void {
    const keysToRemove = [
      'user',
      'userType',
      'isallshops',
      'Audit_Type_Id',
      'Plant_ID',
      'Plant_Code',
      'userid',
      'Manager_ID',
      'Email',
      'Name',
      'shopid',
      'menuPermissions', // Clear menu permissions on logout
    ];

    keysToRemove.forEach((key) => localStorage.removeItem(key));

    // Clear redirect URL using the service
    this.redirectService.clearRedirectUrl();

    this.isAuthenticatedSubject.next(false);
  }

  getUserDisplayName(): string {
    return localStorage.getItem('Name') || 'Unknown User';
  }

  getUserId(): string {
    return localStorage.getItem('Employee_No') || '0';
  }
  getUserDesignation(): string {
    return localStorage.getItem('Designation') || 'Designation';
  }
  getPlantName(): string {
    switch (localStorage.getItem('Plant_Code')?.toUpperCase()) {
      case 'A003':
        return 'Nashik';
      case 'A002':
        return 'Kandivali';
      case 'CK01':
        return 'Chakan';
      case 'A010':
        return 'Haridwar';
      default:
        return localStorage.getItem('Plant_Code') || 'Plant Code';
    }
  }
}
