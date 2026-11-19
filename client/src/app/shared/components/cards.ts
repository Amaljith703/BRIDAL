import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsService } from '../../core/analytics.service';
import { CartService } from '../../core/cart.service';
import {
  Combo,
  PortfolioItem,
  Product,
  Service,
  ServicePackage,
  Testimonial,
  Vlog,
} from '../../core/models';
import { durationLabel } from '../../core/format';
import { IconComponent } from '../icon.component';
import { ImgFallbackDirective } from '../img-fallback.directive';
import { DateLabelPipe, DurationPipe, InrPipe, VideoDurationPipe } from '../pipes';
import { RatingStarsComponent } from './rating-stars.component';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink, IconComponent, ImgFallbackDirective, RatingStarsComponent, InrPipe],
  template: `
    <article class="product-card" [class.is-soldout]="product().stockQuantity <= 0">
      <a class="product-card__media" [routerLink]="['/products', product().slug]" [attr.aria-label]="product().name">
        <img [src]="product().imageUrl" [alt]="product().name" appImgFallback />
        <div class="product-card__badges">
          @if (product().isNewArrival) {
            <span class="badge badge--dark">New</span>
          }
          @if (discount() > 0) {
            <span class="badge badge--gold">−{{ discount() }}%</span>
          }
          @if (product().stockQuantity <= 0) {
            <span class="badge badge--danger">Sold out</span>
          }
        </div>
        <span class="product-card__quick">View edit <app-icon name="arrow-up-right" [size]="13" /></span>
      </a>
      <div class="product-card__body">
        <span class="product-card__category">{{ product().categoryName || product().brand || 'Studio edit' }}</span>
        <a class="product-card__title" [routerLink]="['/products', product().slug]">{{ product().name }}</a>
        @if (product().ratingCount > 0) {
          <app-rating-stars [rating]="product().ratingAverage" [count]="product().ratingCount" />
        } @else {
          <span class="product-card__new">Newly added</span>
        }
        <div class="product-card__foot">
          <span class="price">
            {{ product().price | inr }}
            @if (product().compareAtPrice) {
              <del>{{ product().compareAtPrice | inr }}</del>
            }
          </span>
          <button
            type="button"
            class="product-card__add"
            (click)="add()"
            [disabled]="product().stockQuantity <= 0"
            [attr.aria-label]="'Add ' + product().name + ' to bag'"
          >
            <app-icon name="plus" [size]="16" />
          </button>
        </div>
      </div>
    </article>
  `,
  styles: `
    .product-card {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.1);
      border-radius: 16px;
      overflow: hidden;
      transition:
        transform 0.45s var(--ease),
        box-shadow 0.45s var(--ease),
        border-color 0.45s var(--ease);
    }
    .product-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 30px 55px -38px rgba(61, 42, 47, 0.55);
      border-color: rgba(142, 74, 91, 0.24);
    }
    .product-card__media {
      position: relative;
      display: block;
      aspect-ratio: 4 / 5;
      overflow: hidden;
      background: #f3ebe2;
    }
    .product-card__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.9s var(--ease);
    }
    .product-card:hover .product-card__media img {
      transform: scale(1.06);
    }
    .product-card__badges {
      position: absolute;
      top: 10px;
      left: 10px;
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }
    .product-card__quick {
      position: absolute;
      left: 10px;
      right: 10px;
      bottom: 10px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      padding: 9px;
      border-radius: 999px;
      background: rgba(255, 253, 251, 0.94);
      color: #332226;
      font-size: 0.62rem;
      font-weight: 700;
      letter-spacing: 0.13em;
      text-transform: uppercase;
      opacity: 0;
      transform: translateY(8px);
      transition: 0.35s var(--ease);
      backdrop-filter: blur(4px);
    }
    .product-card:hover .product-card__quick {
      opacity: 1;
      transform: none;
    }
    .product-card__body {
      display: flex;
      flex-direction: column;
      gap: 5px;
      padding: 14px 15px 15px;
      flex: 1;
    }
    .product-card__category {
      font-size: 0.58rem;
      font-weight: 700;
      letter-spacing: 0.16em;
      text-transform: uppercase;
      color: #8d787e;
    }
    .product-card__title {
      font-family: var(--font-display);
      font-size: 1.14rem;
      line-height: 1.18;
      color: #332226;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .product-card__title:hover {
      color: #8e4a5b;
    }
    .product-card__new {
      font-size: 0.66rem;
      color: #8d787e;
    }
    .product-card__foot {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      margin-top: auto;
      padding-top: 8px;
    }
    .product-card__foot .price {
      font-size: 1.1rem;
    }
    .product-card__add {
      display: grid;
      place-items: center;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      border: 1px solid rgba(61, 42, 47, 0.16);
      color: #332226;
      transition: 0.25s var(--ease);
      flex-shrink: 0;
    }
    .product-card__add:hover:not(:disabled) {
      background: #8e4a5b;
      border-color: #8e4a5b;
      color: #fff;
    }
    .product-card__add:disabled {
      opacity: 0.35;
      cursor: not-allowed;
    }
    .product-card.is-soldout .product-card__media img {
      opacity: 0.6;
    }
  `,
})
export class ProductCardComponent {
  readonly product = input.required<Product>();
  private readonly cart = inject(CartService);
  private readonly analytics = inject(AnalyticsService);

  protected readonly discount = computed(() => {
    const item = this.product();
    if (!item.compareAtPrice || item.compareAtPrice <= item.price) return 0;
    return Math.round((1 - item.price / item.compareAtPrice) * 100);
  });

  protected add(): void {
    this.cart.addProduct(this.product());
    this.analytics.track('add_to_cart', {
      metadata: { itemType: 'Product', itemId: this.product().id, name: this.product().name },
    });
  }
}

@Component({
  selector: 'app-service-card',
  imports: [RouterLink, IconComponent, ImgFallbackDirective, DurationPipe, InrPipe],
  template: `
    <article class="service-card">
      <a class="service-card__media" [routerLink]="['/services', service().slug]">
        <img [src]="service().imageUrl" [alt]="service().name" appImgFallback />
        <span class="service-card__duration"><app-icon name="clock" [size]="12" /> {{ service().durationMinutes | duration }}</span>
        @if (service().isFeatured) {
          <span class="service-card__featured">Signature</span>
        }
      </a>
      <div class="service-card__body">
        <span class="service-card__category">{{ service().categoryName || 'Studio service' }}</span>
        <a class="service-card__title" [routerLink]="['/services', service().slug]">{{ service().name }}</a>
        @if (service().shortDescription) {
          <p class="service-card__desc">{{ service().shortDescription }}</p>
        }
        <div class="service-card__foot">
          <span class="price">{{ service().price | inr }}</span>
          <div class="service-card__actions">
            <a class="service-card__book" [routerLink]="['/booking']" [queryParams]="{ service: service().id }">
              Book <app-icon name="arrow-right" [size]="13" />
            </a>
            <button type="button" class="service-card__add" (click)="add()" aria-label="Add service to bag">
              <app-icon name="plus" [size]="15" />
            </button>
          </div>
        </div>
      </div>
    </article>
  `,
  styles: `
    .service-card {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.1);
      border-radius: 18px;
      overflow: hidden;
      transition:
        transform 0.45s var(--ease),
        box-shadow 0.45s var(--ease);
    }
    .service-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 30px 55px -38px rgba(61, 42, 47, 0.5);
    }
    .service-card__media {
      position: relative;
      display: block;
      aspect-ratio: 5 / 4;
      overflow: hidden;
      background: #f3ebe2;
    }
    .service-card__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.9s var(--ease);
    }
    .service-card:hover .service-card__media img {
      transform: scale(1.05);
    }
    .service-card__media::after {
      content: '';
      position: absolute;
      inset: 55% 0 0;
      background: linear-gradient(to top, rgba(30, 18, 22, 0.4), transparent);
      pointer-events: none;
    }
    .service-card__duration {
      position: absolute;
      left: 12px;
      bottom: 12px;
      z-index: 1;
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 5px 10px;
      border-radius: 999px;
      background: rgba(255, 253, 251, 0.92);
      font-size: 0.62rem;
      font-weight: 600;
      color: #4d3a40;
      backdrop-filter: blur(4px);
    }
    .service-card__featured {
      position: absolute;
      top: 12px;
      right: 12px;
      z-index: 1;
      padding: 5px 10px;
      border-radius: 999px;
      background: rgba(201, 162, 107, 0.94);
      color: #2e2024;
      font-size: 0.55rem;
      font-weight: 700;
      letter-spacing: 0.14em;
      text-transform: uppercase;
    }
    .service-card__body {
      display: flex;
      flex-direction: column;
      gap: 7px;
      padding: 17px 17px 18px;
      flex: 1;
    }
    .service-card__category {
      font-size: 0.58rem;
      font-weight: 700;
      letter-spacing: 0.16em;
      text-transform: uppercase;
      color: #8e4a5b;
    }
    .service-card__title {
      font-family: var(--font-display);
      font-size: 1.38rem;
      line-height: 1.15;
      color: #332226;
    }
    .service-card__title:hover {
      color: #8e4a5b;
    }
    .service-card__desc {
      font-size: 0.78rem;
      line-height: 1.6;
      color: #6f5a60;
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .service-card__foot {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      margin-top: auto;
      padding-top: 10px;
    }
    .service-card__actions {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .service-card__book {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      font-size: 0.63rem;
      font-weight: 700;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: #8e4a5b;
    }
    .service-card__book:hover {
      gap: 9px;
    }
    .service-card__add {
      display: grid;
      place-items: center;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      border: 1px solid rgba(61, 42, 47, 0.14);
      color: #332226;
    }
    .service-card__add:hover {
      background: #8e4a5b;
      border-color: #8e4a5b;
      color: #fff;
    }
  `,
})
export class ServiceCardComponent {
  readonly service = input.required<Service>();
  private readonly cart = inject(CartService);

  protected add(): void {
    this.cart.addService(this.service());
  }
}

@Component({
  selector: 'app-package-card',
  imports: [RouterLink, IconComponent, ImgFallbackDirective, DurationPipe, InrPipe],
  template: `
    <article class="package-card" [class.package-card--dark]="dark()">
      <a class="package-card__media" [routerLink]="['/packages', item().slug]">
        <img [src]="item().imageUrl" [alt]="item().name" appImgFallback />
        @if (item().occasion) {
          <span class="package-card__occasion">{{ item().occasion }}</span>
        }
        @if (item().isFeatured) {
          <span class="package-card__featured">Most loved</span>
        }
      </a>
      <div class="package-card__body">
        <div class="package-card__top">
          <span class="package-card__label">Signature package</span>
          @if (item().items.length) {
            <span class="package-card__count">{{ item().items.length }} rituals</span>
          }
        </div>
        <a class="package-card__title" [routerLink]="['/packages', item().slug]">{{ item().name }}</a>
        @if (item().shortDescription) {
          <p class="package-card__desc">{{ item().shortDescription }}</p>
        }
        @if (item().items.length) {
          <ul class="package-card__includes">
            @for (line of item().items.slice(0, 3); track line.id) {
              <li><app-icon name="check" [size]="12" /> {{ line.serviceName }}</li>
            }
          </ul>
        }
        <div class="package-card__foot">
          <div>
            <span class="price">{{ item().price | inr }}</span>
            @if (item().compareAtPrice) {
              <del>{{ item().compareAtPrice | inr }}</del>
            }
          </div>
          <span class="package-card__duration">{{ item().durationMinutes | duration }}</span>
        </div>
        <div class="package-card__actions">
          <a class="btn btn--primary btn--sm" [routerLink]="['/booking']" [queryParams]="{ package: item().id }">Reserve</a>
          <button type="button" class="btn btn--ghost btn--sm" (click)="add()">Add to bag</button>
        </div>
      </div>
    </article>
  `,
  styles: `
    .package-card {
      display: flex;
      flex-direction: column;
      height: 100%;
      border-radius: 20px;
      overflow: hidden;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.12);
      transition:
        transform 0.45s var(--ease),
        box-shadow 0.45s var(--ease);
    }
    .package-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 34px 60px -40px rgba(61, 42, 47, 0.55);
    }
    .package-card__media {
      position: relative;
      display: block;
      aspect-ratio: 16 / 10;
      overflow: hidden;
      background: #f3ebe2;
    }
    .package-card__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.9s var(--ease);
    }
    .package-card:hover .package-card__media img {
      transform: scale(1.05);
    }
    .package-card__occasion,
    .package-card__featured {
      position: absolute;
      top: 12px;
      padding: 5px 11px;
      border-radius: 999px;
      font-size: 0.56rem;
      font-weight: 700;
      letter-spacing: 0.14em;
      text-transform: uppercase;
    }
    .package-card__occasion {
      left: 12px;
      background: rgba(36, 23, 27, 0.75);
      color: #faf6f1;
      backdrop-filter: blur(4px);
    }
    .package-card__featured {
      right: 12px;
      background: #c9a26b;
      color: #2e2024;
    }
    .package-card__body {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 18px;
      flex: 1;
    }
    .package-card__top {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
    }
    .package-card__label {
      font-size: 0.57rem;
      font-weight: 700;
      letter-spacing: 0.16em;
      text-transform: uppercase;
      color: #8e4a5b;
    }
    .package-card__count {
      font-size: 0.62rem;
      color: #8d787e;
    }
    .package-card__title {
      font-family: var(--font-display);
      font-size: 1.5rem;
      line-height: 1.1;
      color: #332226;
    }
    .package-card__title:hover {
      color: #8e4a5b;
    }
    .package-card__desc {
      font-size: 0.78rem;
      line-height: 1.6;
      color: #6f5a60;
      margin: 0;
    }
    .package-card__includes {
      display: grid;
      gap: 4px;
      margin: 2px 0;
    }
    .package-card__includes li {
      display: flex;
      align-items: center;
      gap: 7px;
      font-size: 0.7rem;
      color: #6f5a60;
    }
    .package-card__includes app-icon {
      color: #8e4a5b;
    }
    .package-card__foot {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 10px;
      margin-top: auto;
      padding-top: 8px;
    }
    .package-card__foot del {
      font-size: 0.68rem;
      color: #8d787e;
      margin-left: 6px;
    }
    .package-card__duration {
      font-size: 0.64rem;
      color: #8d787e;
    }
    .package-card__actions {
      display: flex;
      gap: 7px;
      margin-top: 4px;
    }
    .package-card__actions .btn {
      flex: 1;
    }
    .package-card--dark {
      background: rgba(250, 246, 241, 0.04);
      border-color: rgba(250, 246, 241, 0.12);
    }
    .package-card--dark .package-card__title {
      color: #faf6f1;
    }
    .package-card--dark .package-card__desc,
    .package-card--dark .package-card__includes li,
    .package-card--dark .package-card__duration,
    .package-card--dark .package-card__count {
      color: rgba(250, 246, 241, 0.55);
    }
    .package-card--dark .price {
      color: #faf6f1;
    }
  `,
})
export class PackageCardComponent {
  readonly item = input.required<ServicePackage>();
  readonly dark = input(false);
  private readonly cart = inject(CartService);

  protected add(): void {
    this.cart.addPackage(this.item());
  }
}

@Component({
  selector: 'app-testimonial-card',
  imports: [IconComponent, ImgFallbackDirective, RatingStarsComponent],
  template: `
    <article class="testimonial" [class.testimonial--dark]="dark()">
      <span class="testimonial__quote-mark" aria-hidden="true">“</span>
      <app-rating-stars [rating]="item().rating" />
      <blockquote>{{ item().quote }}</blockquote>
      <footer class="testimonial__person">
        @if (item().imageUrl) {
          <img [src]="item().imageUrl" [alt]="item().customerName" appImgFallback />
        } @else {
          <span class="testimonial__avatar">{{ item().customerName.charAt(0) }}</span>
        }
        <span class="testimonial__meta">
          <strong>{{ item().customerName }}</strong>
          @if (item().role) {
            <span>{{ item().role }}</span>
          }
        </span>
        @if (item().videoUrl) {
          <button type="button" class="testimonial__video" (click)="play.emit(item())" aria-label="Play video testimonial">
            <app-icon name="play" [size]="13" />
          </button>
        }
      </footer>
    </article>
  `,
  styles: `
    .testimonial {
      display: flex;
      flex-direction: column;
      gap: 12px;
      height: 100%;
      padding: 26px 24px 22px;
      border-radius: 18px;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.1);
      position: relative;
      overflow: hidden;
    }
    .testimonial__quote-mark {
      position: absolute;
      top: -12px;
      right: 16px;
      font-family: var(--font-display);
      font-size: 7rem;
      line-height: 1;
      color: rgba(142, 74, 91, 0.08);
      pointer-events: none;
    }
    .testimonial blockquote {
      margin: 0;
      font-family: var(--font-display);
      font-size: 1.16rem;
      line-height: 1.5;
      color: #3d2a2f;
      font-style: italic;
      flex: 1;
    }
    .testimonial__person {
      display: flex;
      align-items: center;
      gap: 10px;
      padding-top: 14px;
      border-top: 1px solid rgba(61, 42, 47, 0.08);
    }
    .testimonial__person img,
    .testimonial__avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      object-fit: cover;
      flex-shrink: 0;
    }
    .testimonial__avatar {
      display: grid;
      place-items: center;
      background: #e9c8c1;
      color: #5e2e3c;
      font-family: var(--font-display);
      font-size: 1.1rem;
    }
    .testimonial__meta {
      display: grid;
      gap: 1px;
      flex: 1;
      min-width: 0;
    }
    .testimonial__meta strong {
      font-size: 0.78rem;
      font-weight: 600;
    }
    .testimonial__meta span {
      font-size: 0.66rem;
      color: #8d787e;
    }
    .testimonial__video {
      display: grid;
      place-items: center;
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background: #8e4a5b;
      color: #fff;
      flex-shrink: 0;
    }
    .testimonial--dark {
      background: rgba(250, 246, 241, 0.04);
      border-color: rgba(250, 246, 241, 0.12);
    }
    .testimonial--dark blockquote {
      color: rgba(250, 246, 241, 0.86);
    }
    .testimonial--dark .testimonial__person {
      border-color: rgba(250, 246, 241, 0.12);
    }
    .testimonial--dark .testimonial__meta strong {
      color: #faf6f1;
    }
    .testimonial--dark .testimonial__meta span {
      color: rgba(250, 246, 241, 0.45);
    }
  `,
})
export class TestimonialCardComponent {
  readonly item = input.required<Testimonial>();
  readonly dark = input(false);
  readonly play = output<Testimonial>();
}

@Component({
  selector: 'app-portfolio-card',
  imports: [IconComponent, ImgFallbackDirective],
  template: `
    <button type="button" class="portfolio-tile" (click)="open.emit(item())">
      <img [src]="item().imageUrl" [alt]="item().title" appImgFallback />
      <span class="portfolio-tile__shade"></span>
      <span class="portfolio-tile__copy">
        <span class="portfolio-tile__category">{{ item().category }}</span>
        <strong>{{ item().title }}</strong>
      </span>
      <span class="portfolio-tile__zoom"><app-icon name="plus" [size]="16" /></span>
    </button>
  `,
  styles: `
    .portfolio-tile {
      position: relative;
      display: block;
      width: 100%;
      height: 100%;
      aspect-ratio: 3 / 4;
      overflow: hidden;
      border-radius: 14px;
      background: #f3ebe2;
      text-align: left;
    }
    .portfolio-tile img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.9s var(--ease), filter 0.5s ease;
    }
    .portfolio-tile:hover img {
      transform: scale(1.06);
      filter: brightness(0.92);
    }
    .portfolio-tile__shade {
      position: absolute;
      inset: 40% 0 0;
      background: linear-gradient(to top, rgba(26, 15, 19, 0.78), transparent);
      opacity: 0.65;
      transition: opacity 0.4s ease;
    }
    .portfolio-tile:hover .portfolio-tile__shade {
      opacity: 1;
    }
    .portfolio-tile__copy {
      position: absolute;
      left: 16px;
      right: 16px;
      bottom: 16px;
      display: flex;
      flex-direction: column;
      gap: 3px;
      color: #faf6f1;
    }
    .portfolio-tile__category {
      font-size: 0.56rem;
      font-weight: 700;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      color: #e9c8c1;
    }
    .portfolio-tile__copy strong {
      font-family: var(--font-display);
      font-size: 1.2rem;
      font-weight: 500;
      line-height: 1.2;
    }
    .portfolio-tile__zoom {
      position: absolute;
      top: 12px;
      right: 12px;
      width: 34px;
      height: 34px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background: rgba(255, 253, 251, 0.9);
      color: #332226;
      opacity: 0;
      transform: scale(0.8);
      transition: 0.3s var(--ease);
    }
    .portfolio-tile:hover .portfolio-tile__zoom {
      opacity: 1;
      transform: none;
    }
  `,
})
export class PortfolioCardComponent {
  readonly item = input.required<PortfolioItem>();
  readonly open = output<PortfolioItem>();
}

@Component({
  selector: 'app-vlog-card',
  imports: [RouterLink, IconComponent, ImgFallbackDirective, VideoDurationPipe, DateLabelPipe],
  template: `
    <article class="vlog-card">
      <button type="button" class="vlog-card__media" (click)="play.emit(item())" [attr.aria-label]="'Play ' + item().title">
        <img [src]="item().thumbnailUrl || '/media/seed/vlog-1.jpg'" [alt]="item().title" appImgFallback />
        <span class="vlog-card__badge">{{ item().type }}</span>
        <span class="vlog-card__time">{{ item().durationSeconds | videoDuration }}</span>
        <span class="vlog-card__play"><app-icon name="play" [size]="18" /></span>
      </button>
      <div class="vlog-card__body">
        <span class="vlog-card__eyebrow">
          {{ item().type }} vlog
          @if (item().influencerName) {
            · {{ item().influencerName }}
          }
        </span>
        <a class="vlog-card__title" [routerLink]="['/vlogs', item().slug]">{{ item().title }}</a>
        @if (item().description) {
          <p>{{ item().description }}</p>
        }
        <div class="vlog-card__foot">
          <span>{{ item().publishedAtUtc | dateLabel }}</span>
          <span>{{ item().viewCount }} views</span>
        </div>
      </div>
    </article>
  `,
  styles: `
    .vlog-card {
      display: flex;
      flex-direction: column;
      height: 100%;
      border-radius: 18px;
      overflow: hidden;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.1);
      transition:
        transform 0.4s var(--ease),
        box-shadow 0.4s var(--ease);
    }
    .vlog-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 30px 55px -40px rgba(61, 42, 47, 0.5);
    }
    .vlog-card__media {
      position: relative;
      display: block;
      width: 100%;
      aspect-ratio: 16 / 10;
      overflow: hidden;
      background: #1e1316;
      text-align: left;
    }
    .vlog-card__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      opacity: 0.85;
      transition: transform 0.9s var(--ease), opacity 0.4s ease;
    }
    .vlog-card:hover .vlog-card__media img {
      transform: scale(1.05);
      opacity: 1;
    }
    .vlog-card__badge,
    .vlog-card__time {
      position: absolute;
      top: 12px;
      padding: 5px 10px;
      border-radius: 999px;
      font-size: 0.56rem;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      backdrop-filter: blur(4px);
    }
    .vlog-card__badge {
      left: 12px;
      background: rgba(201, 162, 107, 0.92);
      color: #2e2024;
    }
    .vlog-card__time {
      right: 12px;
      background: rgba(25, 14, 18, 0.72);
      color: #faf6f1;
      letter-spacing: 0.04em;
    }
    .vlog-card__play {
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
      width: 54px;
      height: 54px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background: rgba(255, 253, 251, 0.92);
      color: #332226;
      box-shadow: 0 12px 30px -10px rgba(0, 0, 0, 0.6);
      transition: 0.3s var(--ease);
    }
    .vlog-card:hover .vlog-card__play {
      background: #8e4a5b;
      color: #fff;
      transform: translate(-50%, -50%) scale(1.06);
    }
    .vlog-card__body {
      display: flex;
      flex-direction: column;
      gap: 6px;
      padding: 16px 17px 16px;
      flex: 1;
    }
    .vlog-card__eyebrow {
      font-size: 0.57rem;
      font-weight: 700;
      letter-spacing: 0.15em;
      text-transform: uppercase;
      color: #8e4a5b;
    }
    .vlog-card__title {
      font-family: var(--font-display);
      font-size: 1.32rem;
      line-height: 1.18;
      color: #332226;
    }
    .vlog-card__title:hover {
      color: #8e4a5b;
    }
    .vlog-card__body p {
      font-size: 0.76rem;
      line-height: 1.55;
      color: #6f5a60;
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    .vlog-card__foot {
      display: flex;
      justify-content: space-between;
      gap: 8px;
      margin-top: auto;
      padding-top: 10px;
      font-size: 0.64rem;
      color: #8d787e;
      border-top: 1px solid rgba(61, 42, 47, 0.06);
    }
  `,
})
export class VlogCardComponent {
  readonly item = input.required<Vlog>();
  readonly play = output<Vlog>();
}

@Component({
  selector: 'app-combo-card',
  imports: [RouterLink, IconComponent, ImgFallbackDirective, InrPipe],
  template: `
    <article class="combo-card">
      <a class="combo-card__media" [routerLink]="['/combos']" [queryParams]="{ combo: item().slug }">
        <img [src]="item().imageUrl" [alt]="item().name" appImgFallback />
        <span class="combo-card__tag"><app-icon name="gift" [size]="12" /> Curated set</span>
      </a>
      <div class="combo-card__body">
        <h3>{{ item().name }}</h3>
        @if (item().description) {
          <p>{{ item().description }}</p>
        }
        <ul>
          @for (line of item().items; track line.id) {
            <li>
              <app-icon name="check" [size]="11" />
              {{ line.productName || line.serviceName || line.packageName }}
              @if (line.quantity > 1) {
                <span>× {{ line.quantity }}</span>
              }
            </li>
          }
        </ul>
        <div class="combo-card__foot">
          <span class="price">{{ item().price | inr }}</span>
          <button type="button" class="btn btn--primary btn--sm" (click)="add()">Add set</button>
        </div>
      </div>
    </article>
  `,
  styles: `
    .combo-card {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
      border-radius: 18px;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.1);
    }
    .combo-card__media {
      position: relative;
      display: block;
      aspect-ratio: 16 / 10;
      overflow: hidden;
      background: #f3ebe2;
    }
    .combo-card__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.8s var(--ease);
    }
    .combo-card:hover .combo-card__media img {
      transform: scale(1.05);
    }
    .combo-card__tag {
      position: absolute;
      left: 12px;
      top: 12px;
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 5px 10px;
      border-radius: 999px;
      background: rgba(201, 162, 107, 0.94);
      color: #2e2024;
      font-size: 0.55rem;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
    }
    .combo-card__body {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 18px;
      flex: 1;
    }
    .combo-card__body h3 {
      font-size: 1.4rem;
    }
    .combo-card__body p {
      margin: 0;
      font-size: 0.78rem;
      line-height: 1.6;
      color: #6f5a60;
    }
    .combo-card__body ul {
      display: grid;
      gap: 3px;
      margin: 2px 0;
    }
    .combo-card__body li {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.69rem;
      color: #6f5a60;
    }
    .combo-card__body li app-icon {
      color: #8e4a5b;
    }
    .combo-card__body li span {
      color: #a28d93;
    }
    .combo-card__foot {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      margin-top: auto;
      padding-top: 8px;
    }
  `,
})
export class ComboCardComponent {
  readonly item = input.required<Combo>();
  private readonly cart = inject(CartService);

  protected add(): void {
    this.cart.addCombo(this.item());
  }
}

export const CARD_DURATION = durationLabel;
