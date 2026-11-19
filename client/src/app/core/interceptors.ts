import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api')) return next(req);

  const auth = inject(AuthService);
  const token = auth.accessToken();
  let request = req.clone({ withCredentials: true });
  if (token && !request.headers.has('Authorization')) {
    request = request.clone({ headers: request.headers.set('Authorization', `Bearer ${token}`) });
  }

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !req.url.includes('/auth/') &&
        !request.headers.has('X-Retry-After-Refresh')
      ) {
        return from(auth.refreshSilent()).pipe(
          switchMap((refreshed) => {
            if (!refreshed) return throwError(() => error);
            const freshToken = auth.accessToken();
            const retry = request.clone({
              headers: request.headers
                .set('X-Retry-After-Refresh', '1')
                .set('Authorization', `Bearer ${freshToken ?? ''}`),
            });
            return next(retry);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};

export const serverErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 0) {
          toast.error('The studio server is unreachable. Please check your connection.');
        } else if (error.status === 429) {
          toast.warning('A little too many requests. Please take a breath and try again.');
        } else if (error.status >= 500) {
          toast.error('Something went wrong on our side. The studio team has been notified.');
        } else if (error.status === 401 && !req.url.includes('/auth/')) {
          const returnUrl = router.url;
          if (returnUrl.startsWith('/admin') || returnUrl.startsWith('/account')) {
            toast.info('Your session has expired. Please sign in again.');
          }
        }
      }
      return throwError(() => error);
    }),
  );
};
