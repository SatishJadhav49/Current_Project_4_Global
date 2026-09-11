import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiRequestService } from '../../services';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly apiRequest = inject(ApiRequestService);

}
