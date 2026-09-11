import { Injectable } from '@angular/core';
import {
  HttpClient,
  HttpHeaders,
  HttpParams,
  HttpErrorResponse,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AppConfig } from '../config/app.config.service';
import { ToastService } from './toast.service';

@Injectable({
  providedIn: 'root',
})
export class ApiRequestService {
  apiurl: String;

  constructor(
    private appConfig: AppConfig,
    private http: HttpClient,
    private router: Router,
    private toastService: ToastService
  ) {
    this.apiurl = this.appConfig.baseApiPath;
  }

  /**
   * This is a Global place to add all the request headers for every REST calls
   */
  appendAuthHeader(): HttpHeaders {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    const token = localStorage.getItem('user'); // Changed from 'token' to 'user' as per your auth setup
    if (token !== null) {
      headers = headers.set('Authorization', 'Bearer ' + token);
    }
    return headers;
  }

  /**
   * This is a Global place to define all the Request Headers that must be sent for every ajax call
   */
  getRequestOptions(urlParams?: HttpParams, body?: any) {
    const headers = this.appendAuthHeader();
    const options: any = {
      headers,
      body,
      params: urlParams,
      observe: 'response', // Use 'response' to access the full HTTP response.
    };
    return options;
  }

  get(url: string, urlParams?: HttpParams): Observable<any> {
    const me = this;
    const requestOptions = {
      params: urlParams,
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
    };

    return this.http.get(this.appConfig.baseApiPath + url, requestOptions).pipe(
      map((response: any) => {
        console.log(url);
        if (response.res) {
          if (response.res.isErrorMessage) {
            console.error(response.res.messageDetail);
            return;
          } else {
            return response.datalist;
          }
        } else {
          return response;
        }
      }),
      catchError((error: HttpErrorResponse) => {
        console.log(error);
        if (error.status === 401 || error.status === 403) {
          me.router.navigate(['/no-access']);
        } else if (error.status === 0) {
          me.router.navigate(['/no-access']);
        } else if (error.status === 404) {
          this.toastService.showError('Not Found', error.message);
        }
        this.toastService.showError(
          error.error.res.messageTitle,
          error.error.res.messageDetail
        );
        return throwError(error || 'Server error');
      })
    );
  }

  postForGet(url: string, body: any): Observable<any> {
    const me = this;
    // Check if body is FormData - don't set Content-Type for file uploads
    const isFormData = body instanceof FormData;
    const headers = isFormData
      ? new HttpHeaders() // Don't set Content-Type for FormData
      : new HttpHeaders({ 'Content-Type': 'application/json' });

    const requestOptions = { headers };

    return this.http
      .post(this.appConfig.baseApiPath + url, body, requestOptions)
      .pipe(
        map((response: any) => {
        console.log(url);

          if (response.res) {
            if (response.res.isErrorMessage) {
              console.error(response.res.messageDetail);
              return [];
            } else {
              return response.datalist;
            }
          } else {
            return response;
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            me.router.navigate(['/no-access']);
          }
          if (error.status === 404) {
            this.toastService.showError(
              'Not Found',
              'The requested resource was not found.'
            );
          }
          this.toastService.showError(
            error.error.res.messageTitle,
            error.error.res.messageDetail
          );
          return throwError(error || 'Server error');
        })
      );
  }

  post(url: string, body: any): Observable<any> {
    const me = this;
    const requestOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
    };

    return this.http
      .post(this.appConfig.baseApiPath + url, body, requestOptions)
      .pipe(
        map((response: any) => {
          console.log(response);
          
          if (response.res) {
            if (response.res.isErrorMessage) {
              console.error(response.res.messageDetail);
              return [];
            } else {
              return response.res;
            }
          } else {
            return response;
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            me.router.navigate(['/no-access']);
          } else if (error.status === 404) {
            this.toastService.showError('Not Found', error.message);
          }
          console.error(error.error?.ExceptionMessage || error.message);
          this.toastService.showError(
            error.error.res.messageTitle,
            error.error.res.messageDetail
          );

          return throwError(error || 'Server error');
        })
      );
  }

  postDocuments(url: string, body: any): Observable<any> {
    const me = this;
    const requestOptions = {
      headers: new HttpHeaders(),
    };

    return this.http
      .post(this.appConfig.baseApiPath + url, body, requestOptions)
      .pipe(
        map((response: any) => {
          if (response.res) {
            if (response.res.isErrorMessage) {
              console.error(response.res.messageDetail);
              return [];
            } else {
              return response.res;
            }
          } else {
            return response;
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            me.router.navigate(['/no-access']);
          } else if (error.status === 404) {
            this.toastService.showError('Not Found', error.message);
          }
          console.error(error.error?.ExceptionMessage || error.message);
          this.toastService.showError(
            error.error.res.messageTitle,
            error.error.res.messageDetail
          );

          return throwError(error || 'Server error');
        })
      );
  }

  put(url: string, id: number, body: any): Observable<any> {
    const me = this;
    const requestOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
    };

    return this.http
      .put(this.appConfig.baseApiPath + `${url}/${id}`, body, requestOptions)
      .pipe(
        map((response: any) => {
          if (response.res) {
            if (response.res.isErrorMessage) {
              console.error(response.res.messageDetail);
              return [];
            } else {
              return response.res;
            }
          } else {
            return response;
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            me.router.navigate(['/no-access']);
          } else if (error.status === 404) {
            this.toastService.showError('Not Found', error.message);
          }
          this.toastService.showError(
            error.error.res.messageTitle,
            error.error.res.messageDetail
          );
          return throwError(error || 'Server error');
        })
      );
  }

  delete(url: string, body: any): Observable<any> {
    const me = this;
    const requestOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
      body: body,
    };

    return this.http
      .delete(this.appConfig.baseApiPath + url, requestOptions)
      .pipe(
        map((response: any) => {
          if (response.res) {
            if (response.res.isErrorMessage) {
              console.error(response.res.messageDetail);
              return;
            } else {
              return response.res;
            }
          } else {
            return response;
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            me.router.navigate(['/no-access']);
          } else if (error.status === 404) {
            this.toastService.showError('Not Found', error.message);
          }
          this.toastService.showError(
            error.error.res.messageTitle,
            error.error.res.messageDetail
          );

          return throwError(error || 'Server error');
        })
      );
  }

  /**
   * POST request that returns a blob response (for file downloads)
   * @param url - API endpoint URL
   * @param body - Request body data
   * @returns Observable<Blob>
   */
  postForBlob(url: string, body: any): Observable<Blob> {
    const me = this;
    const requestOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
      responseType: 'blob' as 'json', // Type assertion to satisfy TypeScript
    };

    return this.http
      .post(this.appConfig.baseApiPath + url, body, requestOptions)
      .pipe(
        map((response: any) => {
          console.log(url);
          return response; // Return the blob directly
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            me.router.navigate(['/no-access']);
          } else if (error.status === 404) {
            this.toastService.showError('Not Found', error.message);
          } else {
            this.toastService.showError(
              'Error',
              'Failed to download file. Please try again.'
            );
          }
          return throwError(error || 'Server error');
        })
      );
  }
}
