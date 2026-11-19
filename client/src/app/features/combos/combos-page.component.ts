import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Combo, Paged } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { ComboCardComponent } from '../../shared/components/cards';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { InrPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-combos-page',
  imports: [RouterLink, PageHeroComponent, ComboCardComponent, IconComponent, InrPipe, RevealDirective],
  templateUrl: './combos-page.component.html',
  styleUrl: './combos-page.component.scss',
})
export class CombosPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly combos = signal<Combo[]>([]);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly total = signal(0);

  protected readonly offers = computed(() => this.site.offers());

  protected readonly reasons = [
    { icon: 'gift', title: 'Better than buying apart', copy: 'Every combo is priced below the sum of its pieces — the saving is shown honestly.' },
    { icon: 'brush', title: 'Built by artists', copy: 'Pairings that actually work together on real skin, not just in a photograph.' },
    { icon: 'package', title: 'One delivery', copy: 'Products arrive together, packed by the studio with a note on how to use them.' },
    { icon: 'message', title: 'A routine, explained', copy: 'Each combo includes a simple card with the order of use, morning and night.' },
  ];

  async ngOnInit(): Promise<void> {
    await this.load();
    this.analytics.track('combos_view');
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<Combo>>('catalog/combos', {
        page: 1,
        pageSize: 24,
        search: this.search() || undefined,
      });
      this.combos.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load combos');
      this.combos.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected applySearch(value: string): void {
    this.search.set(value.trim());
    void this.load();
  }
}
