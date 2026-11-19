import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { Faq, Paged } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-faq-page',
  imports: [RouterLink, PageHeroComponent, IconComponent, RevealDirective],
  templateUrl: './faq-page.component.html',
  styleUrl: './faq-page.component.scss',
})
export class FaqPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);

  protected readonly faqs = signal<Faq[]>([]);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly category = signal('All');
  protected readonly open = signal<string | null>(null);

  protected readonly categories = computed(() => {
    const set = new Set(this.faqs().map((item) => item.category).filter(Boolean));
    return ['All', ...[...set].sort()];
  });

  protected readonly filtered = computed(() => {
    const term = this.search().toLowerCase().trim();
    const category = this.category();
    return this.faqs().filter((item) => {
      const matchesCategory = category === 'All' || item.category === category;
      const matchesTerm = !term || item.question.toLowerCase().includes(term) || item.answer.toLowerCase().includes(term);
      return matchesCategory && matchesTerm;
    });
  });

  protected readonly groups = computed(() => {
    const list = this.filtered();
    const map = new Map<string, Faq[]>();
    for (const item of list) {
      const key = item.category || 'General';
      map.set(key, [...(map.get(key) ?? []), item]);
    }
    return [...map.entries()].map(([category, items]) => ({ category, items }));
  });

  protected readonly promises = [
    { icon: 'message', title: 'A real person replies', copy: 'No bots on the studio inbox — usually within one working day.' },
    { icon: 'shield', title: 'Transparent pricing', copy: 'GST, travel and add-ons are itemised before you pay a rupee.' },
    { icon: 'calendar', title: 'Flexible dates', copy: 'Reschedule once with 7 days’ notice; emergencies are handled kindly.' },
  ];

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<Faq>>('content/faqs', { page: 1, pageSize: 100 });
      this.faqs.set(result.items);
      this.open.set(result.items[0]?.id ?? null);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load the FAQ');
      this.faqs.set(this.site.faqs());
    } finally {
      this.loading.set(false);
    }
  }

  protected applySearch(value: string): void {
    this.search.set(value);
  }

  protected setCategory(value: string): void {
    this.category.set(value);
  }

  protected toggle(id: string): void {
    this.open.update((current) => (current === id ? null : id));
  }

  protected clear(): void {
    this.search.set('');
    this.category.set('All');
  }
}
