// @vitest-environment jsdom
import { installStoragePolyfill } from '../../../testing/storage-polyfill';
installStoragePolyfill();

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { vi } from 'vitest';
import { LoginComponent } from './login.component';
import { environment } from '../../../environments/environment';

const loginUrl = `${environment.apiBaseUrl}/auth/login`;

describe('LoginComponent', () => {
  let httpMock: HttpTestingController;
  let router: Router;

  const clearStorage = () => {
    sessionStorage.removeItem('remittance.token');
    sessionStorage.removeItem('remittance.user');
  };

  beforeEach(async () => {
    clearStorage();
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideAnimationsAsync()
      ]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    httpMock.verify();
    clearStorage();
  });

  it('precarga las credenciales del demo en el formulario', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const cmp = fixture.componentInstance;

    expect(cmp.form.value).toEqual({
      email: 'demo@remittance.test',
      password: 'Demo123!'
    });
    expect(cmp.form.valid).toBe(true);
  });

  describe('validaciones (AC-12 aplica también a login)', () => {
    it('marca el form inválido cuando el email no es válido', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const cmp = fixture.componentInstance;

      cmp.form.controls.email.setValue('not-an-email');
      expect(cmp.form.invalid).toBe(true);
    });

    it('marca el form inválido cuando la password tiene menos de 6 chars', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const cmp = fixture.componentInstance;

      cmp.form.controls.password.setValue('123');
      expect(cmp.form.invalid).toBe(true);
    });

    it('submit() es no-op cuando el form es inválido', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const cmp = fixture.componentInstance;
      cmp.form.controls.email.setValue('');

      cmp.submit();

      httpMock.expectNone(loginUrl);
      expect(cmp.loading()).toBe(false);
    });
  });

  describe('AC-02 · login exitoso', () => {
    it('navega a /remittances tras un login OK', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const cmp = fixture.componentInstance;

      cmp.submit();
      const req = httpMock.expectOne(loginUrl);
      req.flush({
        token: 't', userId: 'u', email: 'demo@remittance.test', displayName: 'Demo'
      });

      expect(router.navigate).toHaveBeenCalledWith(['/remittances']);
      expect(cmp.loading()).toBe(false);
      expect(cmp.errorMsg()).toBeNull();
    });
  });

  describe('AC-03 · login fallido muestra mensaje de error', () => {
    it('muestra el detail del backend cuando viene 401', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const cmp = fixture.componentInstance;

      cmp.submit();
      httpMock.expectOne(loginUrl).flush(
        { detail: 'Invalid email or password.' },
        { status: 401, statusText: 'Unauthorized' }
      );

      expect(cmp.errorMsg()).toBe('Invalid email or password.');
      expect(cmp.loading()).toBe(false);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('usa fallback cuando el error no trae detail', () => {
      const fixture = TestBed.createComponent(LoginComponent);
      const cmp = fixture.componentInstance;

      cmp.submit();
      httpMock.expectOne(loginUrl).flush(null, { status: 500, statusText: 'Server' });

      expect(cmp.errorMsg()).toBe('Invalid email or password.');
    });
  });
});

