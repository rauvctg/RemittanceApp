import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateRemittanceRequest, Remittance, TrackingResult, UpdateRemittanceRequest } from '../models/remittance.model';

@Injectable({ providedIn: 'root' })
export class RemittancesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/remittances`;
  private readonly publicUrl = `${environment.apiBaseUrl}/public`;

  list(): Observable<Remittance[]> {
    return this.http.get<Remittance[]>(this.baseUrl);
  }

  getById(id: string): Observable<Remittance> {
    return this.http.get<Remittance>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateRemittanceRequest): Observable<Remittance> {
    return this.http.post<Remittance>(this.baseUrl, request);
  }

  update(id: string, request: UpdateRemittanceRequest): Observable<Remittance> {
    return this.http.put<Remittance>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  track(code: string): Observable<TrackingResult> {
    return this.http.get<TrackingResult>(`${this.publicUrl}/track/${encodeURIComponent(code)}`);
  }
}
