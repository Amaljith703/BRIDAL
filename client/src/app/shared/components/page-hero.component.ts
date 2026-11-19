import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ImgFallbackDirective } from '../img-fallback.directive';

export interface Crumb {
  label: string;
  url?: string;
}

@Component({
  selector: 'app-page-hero',
  imports: [RouterLink, ImgFallbackDirective],
  template: `
    <section class="page-hero" [class.page-hero--compact]="compact()" [class.page-hero--image]="!!imageUrl()">
      @if (imageUrl()) {
        <div class="page-hero__media">
          <img [src]="imageUrl()" [alt]="title()" appImgFallback />
        </div>
      }
      <div class="shell page-hero__inner" [class.page-hero__inner--center]="align() === 'center'">
        @if (crumbs().length) {
          <nav class="breadcrumb" aria-label="Breadcrumb">
            @for (crumb of crumbs(); track crumb.label; let last = $last) {
              @if (crumb.url && !last) {
                <a [routerLink]="crumb.url">{{ crumb.label }}</a>
                <span>/</span>
              } @else {
                <span class="breadcrumb__current">{{ crumb.label }}</span>
              }
            }
          </nav>
        }
        <span class="eyebrow eyebrow--light">{{ eyebrow() }}</span>
        <h1>{{ title() }}</h1>
        @if (subtitle()) {
          <p class="page-hero__subtitle">{{ subtitle() }}</p>
        }
        <div class="page-hero__actions"><ng-content /></div>
      </div>
      <div class="page-hero__grain" aria-hidden="true"></div>
    </section>
  `,
  styles: `
    .page-hero {
      position: relative;
      overflow: hidden;
      background:
        radial-gradient(900px 460px at 82% 10%, rgba(233, 200, 193, 0.2), transparent 65%),
        linear-gradient(120deg, #2e1c22, #4a2832 55%, #332226);
      color: #faf6f1;
      padding-block: clamp(64px, 9vw, 112px) clamp(52px, 7vw, 88px);
      isolation: isolate;
    }
    .page-hero--compact {
      padding-block: clamp(44px, 6vw, 72px) clamp(40px, 5vw, 60px);
    }
    .page-hero__media {
      position: absolute;
      inset: 0;
      z-index: -2;
    }
    .page-hero__media::after {
      content: '';
      position: absolute;
      inset: 0;
      background: linear-gradient(95deg, rgba(36, 23, 27, 0.95) 8%, rgba(36, 23, 27, 0.74) 48%, rgba(36, 23, 27, 0.38) 100%);
    }
    .page-hero__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      opacity: 0.75;
    }
    .page-hero__inner {
      position: relative;
      z-index: 1;
      display: flex;
      flex-direction: column;
      gap: 14px;
      align-items: flex-start;
      max-width: 980px;
    }
    .page-hero__inner--center {
      align-items: center;
      text-align: center;
      margin-inline: auto;
    }
    .page-hero .breadcrumb {
      color: rgba(250, 246, 241, 0.55);
      margin-bottom: 10px;
    }
    .page-hero .breadcrumb a:hover {
      color: #e9c8c1;
    }
    .breadcrumb__current {
      color: rgba(250, 246, 241, 0.82);
      opacity: 1 !important;
    }
    .page-hero h1 {
      color: #faf6f1;
      font-size: clamp(2.6rem, 6vw, 5.2rem);
      max-width: 18ch;
    }
    .page-hero__inner--center h1 {
      max-width: 24ch;
    }
    .page-hero__subtitle {
      margin: 4px 0 0;
      max-width: 62ch;
      color: rgba(250, 246, 241, 0.67);
      font-size: clamp(0.92rem, 1.2vw, 1.05rem);
      line-height: 1.75;
    }
    .page-hero__actions {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      margin-top: 10px;
    }
    .page-hero__grain {
      position: absolute;
      inset: 0;
      z-index: 0;
      pointer-events: none;
      opacity: 0.35;
      background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 160 160' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)' opacity='0.08'/%3E%3C/svg%3E");
      mix-blend-mode: overlay;
    }
    .page-hero--compact h1 {
      font-size: clamp(2.2rem, 4.4vw, 3.9rem);
    }
  `,
})
export class PageHeroComponent {
  readonly eyebrow = input('Makeup Art Studio');
  readonly title = input('');
  readonly subtitle = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly align = input<'left' | 'center'>('left');
  readonly compact = input(false);
  readonly crumbs = input<Crumb[]>([]);
}
