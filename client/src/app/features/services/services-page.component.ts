import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Paged, Service, ServiceCategory } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { ServiceCardComponent } from '../../shared/components/cards';
import { IconComponent } from '../../shared/icon.component';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-services-page',
  imports: [RouterLink, PageHeroComponent, ServiceCardComponent, IconComponent, RevealDirective],
  templateUrl: './services-page.component.html',
  styleUrl: './services-page.component.scss',
})
export class ServicesPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly services = signal<Service[]>([]);
  protected readonly categories = signal<ServiceCategory[]>([]);
  protected readonly loading = signal(true);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly search = signal('');
  protected readonly category = signal('all');
  protected readonly sort = signal('curated');

  protected readonly hasFilters = computed(
    () => !!this.search() || this.category() !== 'all' || this.sort() !== 'curated',
  );

  protected readonly process = [
    {
      icon: 'message',
      title: 'Consult',
      copy: 'A short conversation about your skin, your outfit and the light you will be photographed in.',
    },
    {
      icon: 'styles',
      title: 'Trial',
      copy: 'We test textures and tones together so the day-of look is already familiar — and already yours.',
    },
    {
      icon: 'brush',
      title: 'The day',
      copy: 'A calm, unhurried session with a take-home touch-up plan and a kit list if you want one.',
    },
  ];

  async ngOnInit(): Promise<void> {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('search')) this.search.set(params.get('search') ?? '');
    if (params.get('category')) this.category.set(params.get('category') ?? 'all');
    await Promise.allSettled([this.loadCategories(), this.load()]);
    this.analytics.track('services_view');
  }

  private async loadCategories(): Promise<void> {
    try {
      this.categories.set(await this.api.get<ServiceCategory[]>('catalog/service-categories'));
    } catch {
      // The filter row is optional if categories are briefly unavailable.
    }
  }

  protected async load(reset = false): Promise<void> {
    if (reset) this.page.set(1);
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<Service>>('catalog/services', {
        page: this.page(),
        pageSize: 9,
        search: this.search() || undefined,
        category: this.category() === 'all' ? undefined : this.category(),
        sortBy: this.sort() === 'curated' ? undefined : this.sort(),
        sortDir: this.sort() === 'price-high' || this.sort() === 'name-desc' ? 'desc' : 'asc',
      });
      this.services.set(result.items);
      this.total.set(result.totalCount);
      this.totalPages.set(result.totalPages);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load the menu');
      this.services.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected applySearch(value: string): void {
    this.search.set(value.trim());
    void this.updateUrl();
    void this.load(true);
  }

  protected setCategory(slug: string): void {
    this.category.set(slug);
    void this.updateUrl();
    void this.load(true);
  }

  protected setSort(value: string): void {
    this.sort.set(value);
    void this.load(true);
  }

  protected clear(): void {
    this.search.set('');
    this.category.set('all');
    this.sort.set('curated');
    void this.updateUrl();
    void this.load(true);
  }

  protected goTo(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.page.set(page);
    void this.load();
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 320, behavior: 'smooth' });
    }
  }

  protected pages(): number[] {
    const total = this.totalPages();
    const current = this.page();
    const start = Math.max(1, Math.min(current - 1, total - 2));
    return Array.from({ length: Math.min(3, total) }, (_, index) => start + index).filter((page) => page <= total);
  }

  private async updateUrl(): Promise<void> {
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
