import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import { Product, ProductDetail, ProductImage, ProductVariant } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { ProductCardComponent } from '../../shared/components/cards';
import { RatingStarsComponent } from '../../shared/components/rating-stars.component';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { InrPipe, SafeHtmlPipe, DateLabelPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-product-detail',
  imports: [
    RouterLink,
    ProductCardComponent,
    RatingStarsComponent,
    IconComponent,
    ImgFallbackDirective,
    InrPipe,
    SafeHtmlPipe,
    DateLabelPipe,
    RevealDirective,
  ],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
})
export class ProductDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cart = inject(CartService);
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly auth = inject(AuthService);

  protected readonly product = signal<ProductDetail | null>(null);
  protected readonly related = signal<Product[]>([]);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly quantity = signal(1);
  protected readonly variant = signal<ProductVariant | null>(null);
  protected readonly gallery = signal(0);
  protected readonly tab = signal<'story' | 'ritual' | 'care'>('story');
  protected readonly reviewLimit = signal(3);

  protected readonly fallbackImages = [
    '/media/seed/product-1.jpg',
    '/media/seed/product-2.jpg',
    '/media/seed/product-3.jpg',
  ];

  protected readonly activePrice = computed(() => {
    const variant = this.variant();
    if (variant?.priceOverride) return variant.priceOverride;
    return this.product()?.price ?? 0;
  });

  protected readonly images = computed<ProductImage[]>(() => {
    const product = this.product();
    if (!product) return [];
    if (product.images?.length) return [...product.images].sort((a, b) => a.sortOrder - b.sortOrder);
    const primary = product.imageUrl || this.fallbackImages[0];
    return [
      { id: 'primary', url: primary, altText: product.name, sortOrder: 0 },
      { id: 'fallback-1', url: this.fallbackImages[1], altText: product.name, sortOrder: 1 },
      { id: 'fallback-2', url: this.fallbackImages[2], altText: product.name, sortOrder: 2 },
    ];
  });

  protected readonly currentImage = computed(() => this.images()[this.gallery()]?.url ?? this.product()?.imageUrl ?? '');
  protected readonly savings = computed(() => {
    const product = this.product();
    if (!product?.compareAtPrice || product.compareAtPrice <= product.price) return 0;
    return product.compareAtPrice - product.price;
  });
  protected readonly stockState = computed(() => {
    const variant = this.variant();
    const product = this.product();
    const stock = variant?.stockQuantity ?? product?.stockQuantity ?? 0;
    if (stock <= 0) return { label: 'Sold out', tone: 'out' as const, stock: 0 };
    if (stock <= (product?.lowStockThreshold || 5)) return { label: `Only ${stock} left`, tone: 'low' as const, stock };
    return { label: 'In stock · ships in 24 hours', tone: 'ok' as const, stock };
  });
  protected readonly visibleReviews = computed(() => this.product()?.reviews?.slice(0, this.reviewLimit()) ?? []);

  protected readonly crumbs = computed(() => [
    { label: 'Home', url: '/' },
    { label: 'Shop', url: '/products' },
    { label: this.product()?.name ?? 'Product' },
  ]);

  protected readonly accordion = [
    {
      key: 'shipping' as const,
      title: 'Shipping & returns',
      copy: 'Dispatched from Bengaluru within 24 hours. Free shipping on orders over ₹2,500. Unopened products can be returned within 7 days with the invoice.',
    },
    {
      key: 'authenticity' as const,
      title: 'Authenticity promise',
      copy: 'Sourced directly from authorised distributors. Every batch is checked and stored in a temperature-controlled studio cabinet.',
    },
  ];

  protected readonly openAccordion = signal<string | null>('shipping');

  async ngOnInit(): Promise<void> {
    await this.load(this.route.snapshot.paramMap.get('slug') ?? '');
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    try {
      const product = await this.api.get<ProductDetail>(`catalog/products/${slug}`);
      this.product.set(product);
      const preferred = product.variants?.find((variant) => variant.stockQuantity > 0) ?? product.variants?.[0] ?? null;
      this.variant.set(preferred);
      this.gallery.set(0);
      this.site.setSeo(product.name, product.shortDescription ?? undefined);
      this.analytics.track('product_view', { metadata: { productId: product.id, name: product.name } });
      void this.loadRelated(slug);
    } catch (error) {
      this.product.set(null);
      this.notFound.set(true);
      if (toApiError(error).status !== 404) {
        this.toast.error(toApiError(error).message, 'Could not open the product');
      }
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRelated(slug: string): Promise<void> {
    try {
      this.related.set(await this.api.get<Product[]>(`catalog/products/${slug}/related`));
    } catch {
      this.related.set([]);
    }
  }

  protected selectVariant(variant: ProductVariant): void {
    this.variant.set(variant);
    this.quantity.set(1);
    if (variant.imageUrl) {
      const index = this.images().findIndex((image) => image.url === variant.imageUrl);
      if (index >= 0) this.gallery.set(index);
    }
  }

  protected setQuantity(value: number): void {
    const max = Math.max(1, this.stockState().stock || 1);
    this.quantity.set(Math.max(1, Math.min(max, value)));
  }

  protected addToBag(goToCheckout = false): void {
    const product = this.product();
    if (!product || this.stockState().stock <= 0) return;
    this.cart.addProduct(product, this.variant(), this.quantity());
    this.analytics.track('add_to_cart', {
      metadata: { itemType: 'Product', itemId: product.id, name: product.name, quantity: this.quantity() },
    });
    if (goToCheckout) {
      void this.router.navigate(['/checkout']);
    }
  }

  protected toggleAccordion(key: string): void {
    this.openAccordion.update((current) => (current === key ? null : key));
  }

  protected selectTab(tab: 'story' | 'ritual' | 'care'): void {
    this.tab.set(tab);
  }

  protected showMoreReviews(): void {
    this.reviewLimit.update((limit) => limit + 3);
  }
}
