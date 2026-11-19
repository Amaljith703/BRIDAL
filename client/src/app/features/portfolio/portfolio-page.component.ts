import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Paged, PortfolioItem } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { LightboxItem, MediaLightboxComponent } from '../../shared/components/media-lightbox.component';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-portfolio-page',
  imports: [RouterLink, PageHeroComponent, MediaLightboxComponent, IconComponent, ImgFallbackDirective, RevealDirective],
  templateUrl: './portfolio-page.component.html',
  styleUrl: './portfolio-page.component.scss',
})
export class PortfolioPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly items = signal<PortfolioItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly category = signal('All');
  protected readonly lightbox = signal<LightboxItem | null>(null);
  protected readonly total = signal(0);
  protected readonly categories = computed(() => {
    const set = new Set(this.items().map((item) => item.category));
    return ['All', ...[...set].sort()];
  });
  protected readonly filtered = computed(() => {
    const category = this.category();
    return category === 'All' ? this.items() : this.items().filter((item) => item.category === category);
  });

  async ngOnInit(): Promise<void> {
    await this.load();
    this.analytics.track('portfolio_gallery_view');
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<PortfolioItem>>('content/portfolio', { page: 1, pageSize: 100 });
      this.items.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load the gallery');
      this.items.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected setCategory(value: string): void {
    this.category.set(value);
  }

  protected open(item: PortfolioItem): void {
    this.analytics.track('portfolio_view', { metadata: { itemId: item.id, title: item.title } });
    this.lightbox.set({
      title: item.title,
      imageUrl: item.imageUrl,
      videoUrl: item.videoUrl,
      description: item.description,
      meta: item.category,
    });
  }

  protected isWide(index: number): boolean {
    return index % 7 === 1 || index % 7 === 5;
  }

  protected isTall(index: number): boolean {
    return index % 7 === 3;
  }
}
