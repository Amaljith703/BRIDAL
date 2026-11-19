import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsService } from '../../core/analytics.service';
import { Offer } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-offers-page',
  imports: [RouterLink, PageHeroComponent, IconComponent, RevealDirective],
  templateUrl: './offers-page.component.html',
  styleUrl: './offers-page.component.scss',
})
export class OffersPageComponent implements OnInit {
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);

  protected readonly offers = computed(() => this.site.offers());
  protected readonly featured = computed(() => this.offers()[0] ?? null);
  protected readonly rest = computed(() => this.offers().slice(1));
  protected readonly copied = signal<string | null>(null);

  protected readonly terms = [
    'One offer per order unless the studio confirms otherwise in writing.',
    'Offers cannot be combined with bridal package advances already paid.',
    'Codes are valid through the dates shown and while appointment slots last.',
    'GST is calculated on the discounted value and shown at checkout.',
    'The studio may withdraw or amend an offer for new bookings at any time.',
  ];

  ngOnInit(): void {
    this.analytics.track('offers_view');
  }

  protected valueLabel(offer: Offer): string {
    if (offer.discountType === 'Percentage') return `${offer.discountValue}% off`;
    return `${new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(offer.discountValue)} off`;
  }

  protected appliesLabel(offer: Offer): string {
    switch (offer.appliesTo) {
      case 'Products':
        return 'Products';
      case 'Services':
        return 'Services';
      case 'Packages':
        return 'Packages';
      case 'Combos':
        return 'Combos';
      default:
        return 'Everything';
    }
  }

  protected validity(offer: Offer): string {
    if (!offer.endsAtUtc) return 'No end date · while slots last';
    const date = new Date(offer.endsAtUtc);
    return `Valid until ${date.toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })}`;
  }

  protected async copy(code: string | null | undefined): Promise<void> {
    if (!code) return;
    try {
      await navigator.clipboard.writeText(code);
      this.copied.set(code);
      this.toast.success(`Code ${code} copied. Apply it at checkout.`, 'Offer saved');
      setTimeout(() => this.copied.set(null), 2400);
    } catch {
      this.toast.info(`Your code is ${code}.`, 'Offer code');
    }
  }
}
