import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { CartService } from '../../core/cart.service';
import { Paged, Service } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { ServiceCardComponent } from '../../shared/components/cards';
import { IconComponent } from '../../shared/icon.component';
import { DurationPipe, InrPipe, SafeHtmlPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-service-detail',
  imports: [
    RouterLink,
    PageHeroComponent,
    ServiceCardComponent,
    IconComponent,
    DurationPipe,
    InrPipe,
    SafeHtmlPipe,
    RevealDirective,
  ],
  templateUrl: './service-detail.component.html',
  styleUrl: './service-detail.component.scss',
})
export class ServiceDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly cart = inject(CartService);
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);

  protected readonly service = signal<Service | null>(null);
  protected readonly related = signal<Service[]>([]);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  protected readonly additions = [
    { label: 'Skin prep & barrier care', note: 'Cleanse, tone, hydrate and prime for your skin type.' },
    { label: 'Lashes included', note: 'Strip or individual lashes, chosen to suit the eye shape.' },
    { label: 'Setting for the long day', note: 'Humidity-aware setting so the finish holds through photographs.' },
    { label: 'Take-home touch-up plan', note: 'A printed card with the exact products and placements used.' },
  ];

  protected readonly prep = [
    'Arrive with clean, moisturised skin — no primer or SPF on the day.',
    'Bring inspiration photos and your outfit if you have them.',
    'Let us know about allergies, actives or recent facials in advance.',
  ];

  protected readonly crumbs = computed(() => [
    { label: 'Home', url: '/' },
    { label: 'Services', url: '/services' },
    { label: this.service()?.name ?? 'Service' },
  ]);

  async ngOnInit(): Promise<void> {
    await this.load(this.route.snapshot.paramMap.get('slug') ?? '');
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    try {
      const service = await this.api.get<Service>(`catalog/services/${slug}`);
      this.service.set(service);
      this.site.setSeo(service.name, service.shortDescription ?? undefined);
      this.analytics.track('service_view', { metadata: { serviceId: service.id, name: service.name } });
      this.loadRelated(service);
    } catch (error) {
      this.service.set(null);
      this.notFound.set(true);
      if (toApiError(error).status !== 404) {
        this.toast.error(toApiError(error).message, 'Could not open the service');
      }
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRelated(service: Service): Promise<void> {
    try {
      const result = await this.api.get<Paged<Service>>('catalog/services', { pageSize: 5 });
      this.related.set(result.items.filter((item) => item.id !== service.id).slice(0, 4));
    } catch {
      this.related.set(this.site.featuredServices().filter((item) => item.id !== service.id).slice(0, 4));
    }
  }

  protected addToBag(): void {
    const service = this.service();
    if (!service) return;
    this.cart.addService(service);
    this.analytics.track('add_to_cart', {
      metadata: { itemType: 'Service', itemId: service.id, name: service.name },
    });
  }
}
