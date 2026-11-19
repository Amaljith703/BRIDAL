import { Component, HostListener, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AiAdvisorComponent } from '../shared/components/ai-advisor.component';
import { SiteFooterComponent } from '../shared/components/site-footer.component';
import { SiteHeaderComponent } from '../shared/components/site-header.component';
import { IconComponent } from '../shared/icon.component';

@Component({
  selector: 'app-public-layout',
  imports: [RouterOutlet, SiteHeaderComponent, SiteFooterComponent, AiAdvisorComponent, IconComponent],
  template: `
    <app-site-header />
    <main id="main-content">
      <router-outlet />
    </main>
    <app-site-footer />
    <app-ai-advisor />

    <button
      type="button"
      class="to-top"
      [class.is-visible]="showTop()"
      (click)="scrollTop()"
      aria-label="Back to top"
    >
      <app-icon name="chevron-up" [size]="18" />
    </button>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }
    main {
      flex: 1;
      min-width: 0;
    }
    .to-top {
      position: fixed;
      right: 20px;
      bottom: 22px;
      z-index: 1300;
      width: 44px;
      height: 44px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background: rgba(43, 27, 32, 0.92);
      color: #faf6f1;
      border: 1px solid rgba(233, 200, 193, 0.22);
      box-shadow: 0 18px 36px -18px rgba(20, 10, 14, 0.7);
      opacity: 0;
      visibility: hidden;
      transform: translateY(12px);
      transition: 0.3s var(--ease);
    }
    .to-top.is-visible {
      opacity: 1;
      visibility: visible;
      transform: none;
    }
    .to-top:hover {
      background: #8e4a5b;
    }
    @media (max-width: 700px) {
      .to-top {
        display: none;
      }
    }
  `,
})
export class PublicLayoutComponent {
  protected readonly showTop = signal(false);

  @HostListener('window:scroll')
  onScroll(): void {
    this.showTop.set(window.scrollY > 900);
  }

  protected scrollTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
