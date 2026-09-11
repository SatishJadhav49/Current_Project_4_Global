import { inject, Injectable } from '@angular/core';
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

  getDefectsData(vehicleNo: string): Observable<DefectsData[]> {
    return this.apiRequest.get(
      `MM_Global_Search/GetVehicleDefects/${encodeURIComponent(vehicleNo)}`
    );
  }
}
