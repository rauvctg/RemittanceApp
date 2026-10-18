import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RemittancesService } from './remittances.service';
import { Remittance } from '../models/remittance.model';
import { environment } from '../../environments/environment';

const baseUrl = `${environment.apiBaseUrl}/remittances`;

const sample: Remittance = {
  id: 'r-1',
  userId: 'user-1',
  senderName: 'Ana',
  senderIdType: 'CC',
  senderIdNumber: '123456',
  beneficiaryName: 'Maria',
  beneficiaryIdType: 'CC',
  beneficiaryIdNumber: '654321',
  amount: 100.5,
  currency: 'USD',
  date: '2026-05-01',
  status: 'Pending',
  createdAtUtc: '2026-05-01T00:00:00Z',
  updatedAtUtc: null,
  trackingCode: 'RM-ABCDEF'
};

describe('RemittancesService', () => {
  let service: RemittancesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(RemittancesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('AC-08 · listar mis remesas', () => {
    it('GET /remittances', () => {
      let result: Remittance[] | undefined;
      service.list().subscribe(r => (result = r));

      const req = httpMock.expectOne(baseUrl);
      expect(req.request.method).toBe('GET');
      req.flush([sample]);

      expect(result).toEqual([sample]);
    });
  });

  describe('AC-09 · crear remesa', () => {
    it('POST /remittances con el body del formulario', () => {
      const body = {
        senderName: 'Ana',
        senderIdType: 'CC' as const,
        senderIdNumber: '123456',
        beneficiaryName: 'Carlos',
        beneficiaryIdType: 'CC' as const,
        beneficiaryIdNumber: '654321',
        amount: 80,
        currency: 'EUR'
      };

      service.create(body).subscribe();
      const req = httpMock.expectOne(baseUrl);

      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(body);
      req.flush({ ...sample, ...body });
    });
  });

  describe('AC-10 · editar remesa', () => {
    it('PUT /remittances/:id', () => {
      const body = {
        senderName: 'Ana',
        senderIdType: 'CC' as const,
        senderIdNumber: '123456',
        beneficiaryName: 'Maria',
        beneficiaryIdType: 'CC' as const,
        beneficiaryIdNumber: '654321',
        amount: 150,
        currency: 'USD',
        status: 'Sent' as const
      };

      service.update('r-1', body).subscribe();
      const req = httpMock.expectOne(`${baseUrl}/r-1`);

      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(body);
      req.flush({ ...sample, ...body });
    });
  });

  describe('AC-11 · eliminar remesa', () => {
    it('DELETE /remittances/:id', () => {
      service.delete('r-1').subscribe();
      const req = httpMock.expectOne(`${baseUrl}/r-1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  it('getById hace GET /remittances/:id', () => {
    let result: Remittance | undefined;
    service.getById('r-1').subscribe(r => (result = r));

    const req = httpMock.expectOne(`${baseUrl}/r-1`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);

    expect(result).toEqual(sample);
  });
});
