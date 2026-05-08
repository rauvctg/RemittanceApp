// @vitest-environment jsdom
import { installStoragePolyfill } from '../../testing/storage-polyfill';
installStoragePolyfill();

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import {
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  provideRouter,
  Router
} from '@angular/router';
import { vi } from 'vitest';
import { authGuard, guestGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('Auth guards', () => {
  let auth: AuthService;
  let router: Router;
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  const clearStorage = () => {
    sessionStorage.removeItem('remittance.token');
    sessionStorage.removeItem('remittance.user');
  };

  beforeEach(() => {
    clearStorage();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => clearStorage());

  describe('AC-06 · authGuard protege rutas', () => {
    it('permite acceso si el usuario está autenticado', () => {
      vi.spyOn(auth, 'isAuthenticated').mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => authGuard(route, state));

      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('redirige a /login si NO está autenticado', () => {
      vi.spyOn(auth, 'isAuthenticated').mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => authGuard(route, state));

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('guestGuard mantiene a usuarios autenticados fuera de /login', () => {
    it('permite acceso si NO está autenticado', () => {
      vi.spyOn(auth, 'isAuthenticated').mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => guestGuard(route, state));

      expect(result).toBe(true);
    });

    it('redirige a /remittances si ya hay sesión', () => {
      vi.spyOn(auth, 'isAuthenticated').mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => guestGuard(route, state));

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/remittances']);
    });
  });
});

