import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Category, Paged, Product } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { ProductCardComponent } from '../../shared/components/cards';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-products-page',
  imports: [RouterLink, PageHeroComponent, ProductCardComponent, IconComponent, RevealDirective],
  templateUrl: './products-page.component.html',
  styleUrl: './products-page.component.scss',
})
export class ProductsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly products = signal<Product[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(true);
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly totalPages = signal(1);
  protected readonly search = signal('');
  protected readonly category = signal('all');
  protected readonly sort = signal('curated');
  protected readonly priceBand = signal('all');
  protected readonly openFilters = signal(false);

  protected readonly priceBands = [
    { value: 'all', label: 'Any price' },
    { value: 'under-1000', label: 'Under ₹1,000', min: undefined, max: 999 },
    { value: '1000-2500', label: '₹1,000 – ₹2,500', min: 1000, max: 2500 },
    { value: '2500-4000', label: '₹2,500 – ₹4,000', min: 2500, max: 4000 },
    { value: 'above-4000', label: '₹4,000+', min: 4000, max: undefined },
  ];

  protected readonly perks = [
    { icon: 'truck', title: 'Free shipping over ₹2,500', copy: 'Dispatched within 24 hours from Bengaluru.' },
    { icon: 'shield', title: 'Authentic, artist-tested', copy: 'Every product is used on our own clients first.' },
    { icon: 'refresh', title: 'Easy 7-day returns', copy: 'Unopened products can be returned with the invoice.' },
    { icon: 'message', title: 'Real artist advice', copy: 'Ask the studio before you buy — we will tell you honestly.' },
  ];

  protected readonly hasFilters = computed(
    () => !!this.search() || this.category() !== 'all' || this.priceBand() !== 'all' || this.sort() !== 'curated',
  );

  async ngOnInit(): Promise<void> {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('search')) this.search.set(params.get('search') ?? '');
    if (params.get('category')) this.category.set(params.get('category') ?? 'all');
    await Promise.allSettled([this.loadCategories(), this.load()]);
    this.analytics.track('products_view');
  }

  private async loadCategories(): Promise<void> {
    try {
      this.categories.set(await this.api.get<Category[]>('catalog/categories'));
    } catch {
      // Filters remain usable without the category list.
    }
  }

  protected async load(reset = false): Promise<void> {
    if (reset) this.page.set(1);
    this.loading.set(true);
    const band = this.priceBands.find((item) => item.value === this.priceBand());
    try {
      const result = await this.api.get<Paged<Product>>('catalog/products', {
        page: this.page(),
        pageSize: 12,
        search: this.search() || undefined,
        category: this.category() === 'all' ? undefined : this.category(),
        minPrice: band?.min,
        maxPrice: band?.max,
        sortBy: this.sort() === 'curated' ? undefined : this.sort(),
        sortDir: this.sort() === 'price-high' || this.sort() === 'name-desc' ? 'desc' : 'asc',
      });
      this.products.set(result.items);
      this.total.set(result.totalCount);
      this.totalPages.set(result.totalPages);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load the shop');
      this.products.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected applySearch(value: string): void {
    this.search.set(value.trim());
    void this.syncUrl();
    void this.load(true);
  }

  protected setCategory(value: string): void {
    this.category.set(value);
    void this.syncUrl();
    void this.load(true);
  }

  protected setPriceBand(value: string): void {
    this.priceBand.set(value);
    void this.load(true);
  }

  protected setSort(value: string): void {
    this.sort.set(value);
    void this.load(true);
  }

  protected clear(): void {
    this.search.set('');
    this.category.set('all');
    this.priceBand.set('all');
    this.sort.set('curated');
    void this.syncUrl();
    void this.load(true);
  }

  protected goTo(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.page.set(page);
    void this.load();
    if (typeof window !== 'undefined') window.scrollTo({ top: 340, behavior: 'smooth' });
  }

  protected pages(): number[] {
    const total = this.totalPages();
    const current = this.page();
    const start = Math.max(1, Math.min(current - 1, total - 2));
    return Array.from({ length: Math.min(3, total) }, (_, index) => start + index).filter((page) => page <= total);
  }

  private async syncUrl(): Promise<void> {
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.search() || null,
        category: this.category() === 'all' ? null : this.category(),
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
