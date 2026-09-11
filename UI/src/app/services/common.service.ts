import { inject, Injectable } from '@angular/core';
import { ApiRequestService } from './api-request.service';
import { map, Observable } from 'rxjs';
import {
  Shop,
} from '../Shared/models/common.model';

@Injectable({
  providedIn: 'root',
})
export class CommonService {
  readonly apiRequest = inject(ApiRequestService);

  getShops(): Observable<Shop[]> {
    return this.apiRequest.get('MM_Common/GetShopList');
  }

}
