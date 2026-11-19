import { Injectable, signal } from '@angular/core';

const PREFERENCE_KEY = 'ma_standard_view';

@Injectable({ providedIn: 'root' })
export class ScrollModeService {
  readonly standard = signal(this.readPreference());

  toggle(): void {
    const next = !this.standard();
    this.standard.set(next);
    try {
      localStorage.setItem(PREFERENCE_KEY, String(next));
    } catch {
      // The current tab still keeps the selected view when storage is unavailable.
    }
  }

  private readPreference(): boolean {
    try {
      return localStorage.getItem(PREFERENCE_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
