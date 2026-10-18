// @vitest-environment jsdom
import { installStoragePolyfill } from '../../testing/storage-polyfill';
installStoragePolyfill();

import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let auth: AuthService;
  let router: Router;

  const clearStorage = () => {
    sessionStorage.removeItem('remittance.token');
    sessionStorage.removeItem('remittance.user');
  };

  beforeEach(() => {
    clearStorage();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    clearStorage();
  });

  describe('AC-04 · token se envía en cada request', () => {
    it('agrega Authorization: Bearer <token> cuando hay sesión', () => {
      sessionStorage.setItem('remittance.token', 'abc.def.ghi');

      http.get('/api/foo').subscribe();
      const req = httpMock.expectOne('/api/foo');

      expect(req.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
      req.flush({});
    });

    it('NO agrega Authorization cuando no hay token', () => {
      http.get('/api/foo').subscribe();
      const req = httpMock.expectOne('/api/foo');

      expect(req.request.headers.has('Authorization')).toBe(false);
      req.flush({});
    });
  });

  describe('AC-05 · 401 cierra sesión y redirige a /login', () => {
    it('llama auth.logout() y router.navigate(["/login"]) ante un 401', () => {
      sessionStorage.setItem('remittance.token', 'expired');
      const logoutSpy = vi.spyOn(auth, 'logout');
      const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

      http.get('/api/foo').subscribe({ next: () => {}, error: () => {} });
      httpMock.expectOne('/api/foo').flush(
        { detail: 'Unauthorized' },
        { status: 401, statusText: 'Unauthorized' }
      );

      expect(logoutSpy).toHaveBeenCalled();
      expect(navSpy).toHaveBeenCalledWith(['/login']);
    });

    it('NO redirige cuando el error es 500', () => {
      const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

      http.get('/api/foo').subscribe({ next: () => {}, error: () => {} });
      httpMock.expectOne('/api/foo').flush(
        { detail: 'Boom' },
        { status: 500, statusText: 'Server Error' }
      );

      expect(navSpy).not.toHaveBeenCalled();
    });
  });
});

