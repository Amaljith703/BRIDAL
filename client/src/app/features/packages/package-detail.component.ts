import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { CartService } from '../../core/cart.service';
import { Paged, ServicePackage } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PackageCardComponent } from '../../shared/components/cards';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { DurationPipe, InrPipe, SafeHtmlPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-package-detail',
  imports: [
    RouterLink,
    PageHeroComponent,
    PackageCardComponent,
    IconComponent,
    DurationPipe,
    InrPipe,
    SafeHtmlPipe,
    RevealDirective,
  ],
  templateUrl: './package-detail.component.html',
  styleUrl: './package-detail.component.scss',
})
export class PackageDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly cart = inject(CartService);
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);

  protected readonly item = signal<ServicePackage | null>(null);
  protected readonly related = signal<ServicePackage[]>([]);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  protected readonly timeline = [
    { time: 'Consult', title: 'The conversation', copy: 'Skin, outfit, jewellery and the light of your venue — we plan the look around all of it.' },
    { time: 'Trial', title: 'The rehearsal', copy: 'We test textures and tones, photograph them in daylight and adjust until it feels like you.' },
    { time: 'The day', title: 'The morning', copy: 'Skin prep, colour in thin layers, hair, draping and a final check in your venue light.' },
    { time: 'After', title: 'The touch-up kit', copy: 'You leave with the exact lip, blot and powder needed for the last photograph of the night.' },
  ];

  protected readonly note = [
    'Trial can be added to any package at a preferred rate.',
    'Hair styling included in bridal and reception packages.',
    'Complimentary travel within Bengaluru city limits.',
    'Balance payable on the day; GST invoice issued automatically.',
  ];

  protected readonly crumbs = computed(() => [
    { label: 'Home', url: '/' },
    { label: 'Packages', url: '/packages' },
    { label: this.item()?.name ?? 'Package' },
  ]);

  protected readonly savings = computed(() => {
    const item = this.item();
    if (!item?.compareAtPrice || item.compareAtPrice <= item.price) return 0;
    return item.compareAtPrice - item.price;
  });

  async ngOnInit(): Promise<void> {
    await this.load(this.route.snapshot.paramMap.get('slug') ?? '');
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    try {
      const item = await this.api.get<ServicePackage>(`catalog/packages/${slug}`);
      this.item.set(item);
      this.site.setSeo(item.name, item.shortDescription ?? undefined);
      this.analytics.track('package_view', { metadata: { packageId: item.id, name: item.name } });
      void this.loadRelated();
    } catch (error) {
      this.item.set(null);
      this.notFound.set(true);
      if (toApiError(error).status !== 404) {
        this.toast.error(toApiError(error).message, 'Could not open the package');
      }
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRelated(): Promise<void> {
    try {
      const result = await this.api.get<Paged<ServicePackage>>('catalog/packages', { pageSize: 4 });
      this.related.set(result.items.filter((entry) => entry.id !== this.item()?.id).slice(0, 3));
    } catch {
      this.related.set([]);
    }
  }

  protected addToBag(): void {
    const item = this.item();
    if (!item) return;
    this.cart.addPackage(item);
    this.analytics.track('add_to_cart', { metadata: { itemType: 'Package', itemId: item.id, name: item.name } });
  }
}
