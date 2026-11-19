import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsService } from '../../core/analytics.service';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import { CartLine } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ProductCardComponent } from '../../shared/components/cards';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { InrPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-cart-page',
  imports: [RouterLink, ProductCardComponent, IconComponent, ImgFallbackDirective, InrPipe, RevealDirective],
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.scss',
})
export class CartPageComponent {
  private readonly analytics = inject(AnalyticsService);
  protected readonly cart = inject(CartService);
  protected readonly auth = inject(AuthService);
  protected readonly site = inject(SiteService);

  protected readonly freeShippingThreshold = 2500;
  protected readonly shippingEstimate = 99;

  protected readonly physicalSubtotal = computed(() =>
    this.cart
      .lines()
      .filter((line) => line.itemType === 'Product' || line.itemType === 'Combo')
      .reduce((total, line) => total + line.unitPrice * line.quantity, 0),
  );
  protected readonly qualifiesForFreeShipping = computed(() => this.physicalSubtotal() >= this.freeShippingThreshold);
  protected readonly remainingForFreeShipping = computed(() =>
    Math.max(0, this.freeShippingThreshold - this.physicalSubtotal()),
  );
  protected readonly shippingProgress = computed(() =>
    Math.min(100, Math.round((this.physicalSubtotal() / this.freeShippingThreshold) * 100)),
  );
  protected readonly estimatedShipping = computed(() =>
    this.cart.hasPhysicalItems() && !this.qualifiesForFreeShipping() ? this.shippingEstimate : 0,
  );
  protected readonly estimatedTotal = computed(() => this.cart.subtotal() + this.estimatedShipping());

  protected readonly perks = [
    { icon: 'shield', title: 'Secure checkout', copy: 'Razorpay · UPI, cards, netbanking' },
    { icon: 'truck', title: 'Free shipping over ₹2,500', copy: 'Dispatched within 24 hours' },
    { icon: 'refresh', title: '7-day returns', copy: 'On unopened products' },
  ];

  protected readonly suggestions = computed(() =>
    this.site
      .featuredProducts()
      .filter((product) => !this.cart.lines().some((line) => line.itemId === product.id))
      .slice(0, 4),
  );

  protected linkFor(line: CartLine): string {
    switch (line.itemType) {
      case 'Product':
        return `/products/${line.slug}`;
      case 'Service':
        return `/services/${line.slug}`;
      case 'Package':
        return `/packages/${line.slug}`;
      case 'Combo':
        return `/combos`;
      default:
        return '/products';
    }
  }

  protected typeLabel(line: CartLine): string {
    switch (line.itemType) {
      case 'Product':
        return 'Product';
      case 'Service':
        return 'Service';
      case 'Package':
        return 'Package';
      case 'Combo':
        return 'Combo';
      default:
        return line.itemType;
    }
  }

  protected lineTotal(line: CartLine): number {
    return line.unitPrice * line.quantity;
  }

  protected isBookable(line: CartLine): boolean {
    return line.itemType === 'Service' || line.itemType === 'Package';
  }

  protected bump(line: CartLine, action: 'increment' | 'decrement'): void {
    if (action === 'increment') this.cart.increment(line.key);
    else this.cart.decrement(line.key);
    this.analytics.track(`cart_${action}`, { metadata: { itemId: line.itemId, name: line.name } });
  }

  protected removeLine(line: CartLine): void {
    this.cart.remove(line.key);
    this.analytics.track('cart_remove', { metadata: { itemId: line.itemId, name: line.name } });
  }

  protected track(line: CartLine, action: 'increment' | 'decrement' | 'remove'): void {
    this.analytics.track(`cart_${action}`, { metadata: { itemId: line.itemId, name: line.name } });
  }

  protected clearBag(): void {
    if (!this.cart.lines().length) return;
    this.cart.clear();
    this.analytics.track('cart_clear');
  }
}
