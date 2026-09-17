import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiRequestService, AppConfig } from '../../services';
import { DefectsData, DefectsSummaryResponse, VehicleInfo } from './reports.model';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly apiRequest = inject(ApiRequestService);
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(AppConfig);

  getVehicleInfo(vehicleNo: string): Observable<VehicleInfo> {
    return this.apiRequest.get(
      `MM_Global_Search/GetVehicleInfo/${encodeURIComponent(vehicleNo)}`
    );
  }

  getDefectsData(vinNumber: string, biwNo: string): Observable<DefectsData[]> {
    const params = new HttpParams()
      .set('vinNumber', vinNumber ?? '')
      .set('biwNo', biwNo ?? '');

    return this.apiRequest.get('MM_Global_Search/GetVehicleDefects', params);
  }

  /**
   * AI defect summary - served by the python service, so it does not use the
   * ApiRequestService envelope handling.
   */
  getDefectsSummary(
    defects: DefectsData[]
  ): Observable<DefectsSummaryResponse> {
    return this.http.post<DefectsSummaryResponse>(
      `${this.appConfig.aiApiPath}global-search-summary`,
      defects
    );
  }
}
