import { Injectable } from '@angular/core';
import { ApiRequestService } from '../../services';
import { Observable } from 'rxjs';


@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  constructor(private apiRequest: ApiRequestService) {}

}
