import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly sessionId = this.readSession();
  private started = false;

  start(): void {
    if (this.started) return;
    this.started = true;
    this.router.events.pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd)).subscribe((event) => {
      this.track('page_view', { path: event.urlAfterRedirects });
    });
  }

  track(eventType: string, extra: { path?: string; metadata?: Record<string, unknown> } = {}): void {
    if (typeof window === 'undefined') return;
    const path = extra.path ?? this.router.url ?? '/';
    if (path.startsWith('/admin')) return;
    this.api.ping('analytics/events', {
      eventType,
      path: path.split('?')[0],
      sessionId: this.sessionId,
      referrer: document.referrer || null,
      deviceType: window.innerWidth < 700 ? 'mobile' : window.innerWidth < 1100 ? 'tablet' : 'desktop',
      metadataJson: extra.metadata ? JSON.stringify(extra.metadata) : null,
    });
  }

  private readSession(): string {
    try {
      const key = 'ma_analytics_session';
      const existing = sessionStorage.getItem(key);
      if (existing) return existing;
      const next = typeof crypto !== 'undefined' && 'randomUUID' in crypto ? crypto.randomUUID() : Math.random().toString(36).slice(2);
      sessionStorage.setItem(key, next);
      return next;
    } catch {
      return Math.random().toString(36).slice(2);
    }
  }
}
