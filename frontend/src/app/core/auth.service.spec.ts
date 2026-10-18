// @vitest-environment jsdom
import { installStoragePolyfill } from '../../testing/storage-polyfill';
installStoragePolyfill();

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { AuthResponse } from '../models/auth.model';
import { environment } from '../../environments/environment';

const baseUrl = `${environment.apiBaseUrl}/auth`;

const fakeResponse: AuthResponse = {
  token: 'jwt.token.value',
  userId: 'user-1',
  email: 'demo@remittance.test',
  displayName: 'Demo User'
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const clearStorage = () => {
    sessionStorage.removeItem('remittance.token');
    sessionStorage.removeItem('remittance.user');
  };

  beforeEach(() => {
    clearStorage();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    clearStorage();
  });

  describe('AC-02 · login con credenciales válidas', () => {
    it('persiste token y perfil, y marca isAuthenticated()=true', () => {
      let received: AuthResponse | undefined;

      service.login({ email: 'demo@remittance.test', password: 'Demo123!' })
        .subscribe(res => (received = res));

      const req = httpMock.expectOne(`${baseUrl}/login`);
      expect(req.request.method).toBe('POST');
      req.flush(fakeResponse);

      expect(received).toEqual(fakeResponse);
      expect(sessionStorage.getItem('remittance.token')).toBe('jwt.token.value');
      expect(JSON.parse(sessionStorage.getItem('remittance.user')!)).toEqual(fakeResponse);
      expect(service.isAuthenticated()).toBe(true);
      expect(service.user()).toEqual(fakeResponse);
    });
  });

  describe('AC-03 · login con credenciales inválidas', () => {
    it('NO persiste nada cuando el backend responde 401', () => {
      let errorStatus = 0;

      service.login({ email: 'x@y.com', password: 'bad' }).subscribe({
        next: () => {},
        error: err => (errorStatus = err.status)
      });

      const req = httpMock.expectOne(`${baseUrl}/login`);
      req.flush({ detail: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });

      expect(errorStatus).toBe(401);
      expect(sessionStorage.getItem('remittance.token')).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('AC-01 · registro de usuario', () => {
    it('hace POST a /auth/register y devuelve userId', () => {
      let received: { userId: string } | undefined;

      service.register({ email: 'new@test.com', password: 'Secret123!', displayName: 'New' })
        .subscribe(res => (received = res));

      const req = httpMock.expectOne(`${baseUrl}/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        email: 'new@test.com',
        password: 'Secret123!',
        displayName: 'New'
      });
      req.flush({ userId: 'user-2' });

      expect(received).toEqual({ userId: 'user-2' });
    });

    it('NO persiste sesión automáticamente tras registrar', () => {
      service.register({ email: 'a@b.com', password: 'Secret123!', displayName: 'A' }).subscribe();
      httpMock.expectOne(`${baseUrl}/register`).flush({ userId: 'u' });

      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('AC-07 · logout', () => {
    it('limpia token, perfil y baja isAuthenticated() a false', () => {
      service.login({ email: 'demo@remittance.test', password: 'Demo123!' }).subscribe();
      httpMock.expectOne(`${baseUrl}/login`).flush(fakeResponse);
      expect(service.isAuthenticated()).toBe(true);

      service.logout();

      expect(sessionStorage.getItem('remittance.token')).toBeNull();
      expect(sessionStorage.getItem('remittance.user')).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
      expect(service.user()).toBeNull();
    });
  });

  describe('rehidratación de sesión', () => {
    it('al construir el servicio rehidrata el usuario desde sessionStorage', () => {
      sessionStorage.setItem('remittance.token', fakeResponse.token);
      sessionStorage.setItem('remittance.user', JSON.stringify(fakeResponse));

      // Reset TestBed to force a new AuthService instance
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting()]
      });
      const fresh = TestBed.inject(AuthService);

      expect(fresh.isAuthenticated()).toBe(true);
      expect(fresh.user()).toEqual(fakeResponse);
      expect(fresh.getToken()).toBe(fakeResponse.token);

      TestBed.inject(HttpTestingController).verify();
    });
  });
});

