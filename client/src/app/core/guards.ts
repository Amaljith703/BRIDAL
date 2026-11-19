import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

async function ensureAuth(): Promise<AuthService> {
  const auth = inject(AuthService);
  await auth.init();
  return auth;
}

export const authGuard: CanActivateFn = async (route, state): Promise<boolean | UrlTree> => {
  const auth = await ensureAuth();
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const staffGuard: CanActivateFn = async (route, state): Promise<boolean | UrlTree> => {
  const auth = await ensureAuth();
  const router = inject(Router);
  const toast = inject(ToastService);
  if (auth.isStaff()) return true;
  if (auth.isAuthenticated()) {
    toast.warning('The studio dashboard is for staff accounts. You are signed in as a customer.', 'Staff access only');
    return router.createUrlTree(['/account']);
  }
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url, staff: '1' } });
};

export const guestGuard: CanActivateFn = async (): Promise<boolean | UrlTree> => {
  const auth = await ensureAuth();
  const router = inject(Router);
  return auth.isAuthenticated() ? router.createUrlTree([auth.isStaff() ? '/admin' : '/account']) : true;
};
