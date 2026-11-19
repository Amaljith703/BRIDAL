import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiError, ApiService, toApiError } from './api.service';
import {
  AuthResponse,
  TwoFactorSetup,
  User,
} from './models';

const TOKEN_KEY = 'ma_access_token';
const USER_KEY = 'ma_user';
const EXPIRES_KEY = 'ma_token_expires';

export interface LoginPayload {
  email: string;
  password: string;
  twoFactorCode?: string | null;
  rememberMe?: boolean;
}

export interface RegisterPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string | null;
  marketingOptIn?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);

  readonly user = signal<User | null>(this.readStoredUser());
  readonly accessToken = signal<string | null>(this.readStorage(TOKEN_KEY));
  readonly isAuthenticated = computed(() => !!this.user());
  readonly isStaff = computed(() => {
    const roles = this.user()?.roles ?? [];
    return roles.some((role) => ['Admin', 'Manager', 'Support'].includes(role));
  });
  readonly isAdmin = computed(() => (this.user()?.roles ?? []).includes('Admin'));
  readonly isCustomer = computed(() => !!this.user());

  private refreshPromise: Promise<boolean> | null = null;
  private initPromise: Promise<void> | null = null;

  init(): Promise<void> {
    this.initPromise ??= this.initialize();
    return this.initPromise;
  }

  private async initialize(): Promise<void> {
    const token = this.accessToken();
    if (token) {
      try {
        const user = await this.api.get<User>('auth/me');
        this.setSession(this.accessToken(), user);
        return;
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 401) {
          // Keep the locally cached session if the API is temporarily unreachable.
          return;
        }
      }
    }
    try {
      await this.refreshSilent();
    } catch {
      this.clearSession();
    }
  }

  async login(payload: LoginPayload): Promise<AuthResponse> {
    const response = await this.api.post<AuthResponse>('auth/login', payload);
    if (response.accessToken && response.user) this.setSession(response.accessToken, response.user);
    return response;
  }

  async register(payload: RegisterPayload): Promise<AuthResponse> {
    const response = await this.api.post<AuthResponse>('auth/register', payload);
    if (response.accessToken && response.user) this.setSession(response.accessToken, response.user);
    return response;
  }

  async refresh(): Promise<AuthResponse> {
    const response = await this.api.post<AuthResponse>('auth/refresh');
    if (response.accessToken && response.user) this.setSession(response.accessToken, response.user);
    return response;
  }

  async refreshSilent(): Promise<boolean> {
    this.refreshPromise ??= (async () => {
      try {
        const response = await this.api.post<AuthResponse>('auth/refresh');
        if (response.accessToken && response.user) {
          this.setSession(response.accessToken, response.user);
          return true;
        }
        return false;
      } catch {
        this.clearSession();
        return false;
      } finally {
        this.refreshPromise = null;
      }
    })();
    return this.refreshPromise;
  }

  async logout(): Promise<void> {
    try {
      await this.api.post('auth/logout');
    } catch {
      // Local sign-out should always succeed.
    }
    this.clearSession();
  }

  async updateProfile(payload: {
    firstName: string;
    lastName: string;
    phone?: string | null;
    profileImageUrl?: string | null;
    marketingOptIn: boolean;
  }): Promise<User> {
    const user = await this.api.put<User>('auth/profile', payload);
    this.setSession(this.accessToken(), user);
    return user;
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await this.api.post('auth/change-password', { currentPassword, newPassword });
    // Changing the password revokes every refresh token, including this browser's.
    this.clearSession();
  }

  forgotPassword(email: string): Promise<{ message: string }> {
    return this.api.post('auth/forgot-password', { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Promise<{ message: string }> {
    return this.api.post('auth/reset-password', { email, token, newPassword });
  }

  confirmEmail(email: string, token: string): Promise<{ message: string }> {
    return this.api.post('auth/confirm-email', { email, token });
  }

  resendConfirmation(): Promise<{ message: string }> {
    return this.api.post('auth/resend-confirmation');
  }

  twoFactorSetup(): Promise<TwoFactorSetup> {
    return this.api.get('auth/2fa/setup');
  }

  enableTwoFactor(code: string, password?: string): Promise<{ message: string; recoveryCodes: string[] }> {
    return this.api.post('auth/2fa/enable', { code, password });
  }

  disableTwoFactor(password: string, code = '000000'): Promise<{ message: string }> {
    return this.api.post('auth/2fa/disable', { password, code });
  }

  regenerateRecoveryCodes(password: string): Promise<{ recoveryCodes: string[] }> {
    return this.api.post('auth/2fa/recovery-codes', { password, code: '000000' });
  }

  hasRole(...roles: string[]): boolean {
    const current = this.user()?.roles ?? [];
    return roles.some((role) => current.includes(role));
  }

  private setSession(token: string | null, user: User | null, expiresAt?: string | null): void {
    this.accessToken.set(token);
    this.user.set(user);
    if (token) this.writeStorage(TOKEN_KEY, token);
    else this.removeStorage(TOKEN_KEY);
    if (user) this.writeStorage(USER_KEY, JSON.stringify(user));
    else this.removeStorage(USER_KEY);
    if (expiresAt) this.writeStorage(EXPIRES_KEY, expiresAt);
    else this.removeStorage(EXPIRES_KEY);
  }

  private clearSession(): void {
    this.accessToken.set(null);
    this.user.set(null);
    this.removeStorage(TOKEN_KEY);
    this.removeStorage(USER_KEY);
    this.removeStorage(EXPIRES_KEY);
  }

  private readStoredUser(): User | null {
    const raw = this.readStorage(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }

  private readStorage(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private writeStorage(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // Storage can be unavailable in private modes; the in-memory session still works.
    }
  }

  private removeStorage(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch {
      // ignore
    }
  }
}

export function authErrorMessage(error: unknown): string {
  return toApiError(error).message;
}
