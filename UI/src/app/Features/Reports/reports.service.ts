import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiRequestService } from '../../services';
import { DefectsData, VehicleInfo } from './reports.model';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly apiRequest = inject(ApiRequestService);

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
}
