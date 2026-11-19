import { Component, input } from '@angular/core';

@Component({
  selector: 'app-rating-stars',
  template: `
    <span class="rating" [attr.aria-label]="rating() + ' out of 5 stars'">
      <span class="rating__stars" aria-hidden="true">
        <span class="rating__base">★★★★★</span>
        <span class="rating__fill" [style.width.%]="percentage()">★★★★★</span>
      </span>
      @if (showValue()) {
        <span class="rating__value">{{ rating().toFixed(1) }}</span>
      }
      @if (count()) {
        <span class="rating__count">({{ count() }})</span>
      }
    </span>
  `,
  styles: `
    .rating {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 0.72rem;
      color: #6f5a60;
    }
    .rating__stars {
      position: relative;
      display: inline-block;
      letter-spacing: 1px;
      font-size: 0.86rem;
      line-height: 1;
      white-space: nowrap;
    }
    .rating__base {
      color: rgba(61, 42, 47, 0.18);
    }
    .rating__fill {
      position: absolute;
      inset: 0 auto 0 0;
      overflow: hidden;
      color: #c9a26b;
      white-space: nowrap;
    }
    .rating__value {
      font-weight: 600;
      color: #332226;
      font-size: 0.72rem;
    }
    .rating__count {
      font-size: 0.66rem;
      color: #8d787e;
    }
  `,
})
export class RatingStarsComponent {
  readonly rating = input(0);
  readonly count = input(0);
  readonly showValue = input(false);

  protected percentage(): number {
    return Math.max(0, Math.min(100, (this.rating() / 5) * 100));
  }
}
