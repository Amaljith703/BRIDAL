import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Paged, ServicePackage } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PackageCardComponent } from '../../shared/components/cards';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { InrPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-packages-page',
  imports: [RouterLink, PageHeroComponent, PackageCardComponent, IconComponent, InrPipe, RevealDirective],
  templateUrl: './packages-page.component.html',
  styleUrl: './packages-page.component.scss',
})
export class PackagesPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly packages = signal<ServicePackage[]>([]);
  protected readonly loading = signal(true);
  protected readonly total = signal(0);
  protected readonly occasion = signal('all');
  protected readonly sort = signal('curated');

  protected readonly occasions = [
    { value: 'all', label: 'All packages' },
    { value: 'Bridal', label: 'Bridal' },
    { value: 'Reception', label: 'Reception' },
    { value: 'Editorial', label: 'Editorial' },
    { value: 'Occasion', label: 'Occasion' },
  ];

  protected readonly included = [
    { icon: 'brush', title: 'A senior artist', copy: 'Your lead artist stays with you from prep to the final touch-up.' },
    { icon: 'clock', title: 'Unhurried timings', copy: 'Morning slots paced around your event, never back-to-back.' },
    { icon: 'gem', title: 'Premium kit', copy: 'Luxury and professional products suited to Indian skin and climate.' },
    { icon: 'map-pin', title: 'Travel included', copy: 'Complimentary travel within Bengaluru city limits.' },
    { icon: 'package', title: 'Touch-up kit', copy: 'A take-home pouch with the lip, blot and powder you will need.' },
    { icon: 'shield', title: 'Patch-test first', copy: 'A trial or patch test before the day for total peace of mind.' },
  ];

  protected readonly faq = [
    {
      q: 'Can I customise a package?',
      a: 'Absolutely. Packages are starting points. Add a family member, extra draping, a hair change or an outstation day and the studio will re-quote transparently.',
    },
    {
      q: 'What is the booking advance?',
      a: 'A 30% advance reserves your date, with the balance due on the day. Trials are booked separately unless included in your package.',
    },
    {
      q: 'Do packages include hair?',
      a: 'Bridal and reception packages include hair styling. Editorial and occasion packages can add it on request.',
    },
    {
      q: 'How far in advance should I book?',
      a: 'Bridal dates are usually reserved 3–9 months ahead, especially in wedding season. Weekday and occasion bookings can often be accommodated within two weeks.',
    },
  ];

  protected readonly openFaq = signal(0);
  protected readonly startingPrice = computed(() =>
    this.packages().length ? Math.min(...this.packages().map((item) => item.price)) : 0,
  );

  async ngOnInit(): Promise<void> {
    await this.load();
    this.analytics.track('packages_view');
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<ServicePackage>>('catalog/packages', {
        pageSize: 24,
        page: 1,
        occasion: this.occasion() === 'all' ? undefined : this.occasion(),
        sortBy: this.sort() === 'curated' ? undefined : this.sort(),
        sortDir: this.sort() === 'price-high' ? 'desc' : 'asc',
      });
      this.packages.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load packages');
      this.packages.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected setOccasion(value: string): void {
    this.occasion.set(value);
    void this.load();
  }

  protected setSort(value: string): void {
    this.sort.set(value);
    void this.load();
  }

  protected toggleFaq(index: number): void {
    this.openFaq.update((open) => (open === index ? -1 : index));
  }
}
