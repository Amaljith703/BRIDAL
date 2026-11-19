import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withRouterConfig,
} from '@angular/router';
import { routes } from './app.routes';
import { AuthService } from './core/auth.service';
import { authInterceptor, serverErrorInterceptor } from './core/interceptors';
import { MusicService } from './core/music.service';
import { SiteService } from './core/site.service';
import { AnalyticsService } from './core/analytics.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' }),
      withRouterConfig({ paramsInheritanceStrategy: 'always' }),
    ),
    provideHttpClient(withInterceptors([authInterceptor, serverErrorInterceptor])),
    provideAppInitializer(() => {
      const site = inject(SiteService);
      const auth = inject(AuthService);
      const music = inject(MusicService);
      const analytics = inject(AnalyticsService);
      analytics.start();
      return Promise.allSettled([
        site.load().then(() => music.init()),
        auth.init(),
      ]);
    }),
  ],
};
