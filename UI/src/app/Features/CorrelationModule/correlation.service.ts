import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiRequestService } from '../../services';
import {
  CrApplication,
  CrMappingSave,
  CrMasterData,
  CrModel,
  CrParameter,
  CrParameterDelete,
  CrParameterMappings,
  CrParameterSave,
  CrResult,
  CrUnmap,
} from './correlation.model';
import { CrReport, CrReportFilter } from './report/report.model';

@Injectable({
  providedIn: 'root',
})
export class CorrelationService {
  private readonly apiRequest = inject(ApiRequestService);
  private readonly baseUrl = 'MM_CR_Mapping/';

  getModels(): Observable<CrModel[]> {
    return this.apiRequest.get(`${this.baseUrl}GetModels`);
  }

  getApplications(): Observable<CrApplication[]> {
    return this.apiRequest.get(`${this.baseUrl}GetApplications`);
  }

  getParameters(modelId: number): Observable<CrParameter[]> {
    const params = new HttpParams().set('modelId', modelId);
    return this.apiRequest.get(`${this.baseUrl}GetParameters`, params);
  }

  saveParameter(parameter: CrParameterSave): Observable<CrResult> {
    return this.apiRequest.postForGet(
      `${this.baseUrl}SaveParameter`,
      this.withUser(parameter)
    );
  }

  deleteParameter(parameter: CrParameterDelete): Observable<CrResult> {
    return this.apiRequest.postForGet(
      `${this.baseUrl}DeleteParameter`,
      this.withUser(parameter)
    );
  }

  getParameterMappings(crParameterId: number): Observable<CrParameterMappings> {
    return this.apiRequest.get(
      `${this.baseUrl}GetParameterMappings/${crParameterId}`
    );
  }

  getMasterData(appId: number, crParameterId: number): Observable<CrMasterData> {
    const params = new HttpParams()
      .set('appId', appId)
      .set('crParameterId', crParameterId);
    return this.apiRequest.get(`${this.baseUrl}GetMasterData`, params);
  }

  saveMappings(mapping: CrMappingSave): Observable<CrResult> {
    return this.apiRequest.postForGet(
      `${this.baseUrl}SaveMappings`,
      this.withUser(mapping)
    );
  }

  unmap(unmap: CrUnmap): Observable<CrResult> {
    return this.apiRequest.postForGet(
      `${this.baseUrl}Unmap`,
      this.withUser(unmap)
    );
  }

  //*************************************** Report ***************************************//

  getReport(crParameterId: number, filter: CrReportFilter): Observable<CrReport> {
    let params = new HttpParams().set('crParameterId', crParameterId);
    params =
      'lastN' in filter
        ? params.set('lastN', filter.lastN)
        : params.set('fromDate', filter.fromDate).set('toDate', filter.toDate);
    return this.apiRequest.get('MM_CR_Report/GetReport', params);
  }

  /** Stamps the logged-in user and host, same as the other masters. */
  private withUser<T extends object>(body: T): T {
    const employeeId = Number(localStorage.getItem('Employee_ID'));
    return {
      ...body,
      User_ID: isNaN(employeeId) || !employeeId ? null : employeeId,
      Host: localStorage.getItem('Hostname') ?? '',
    };
  }
}
