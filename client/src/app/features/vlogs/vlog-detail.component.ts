import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Paged, Vlog } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { VlogCardComponent } from '../../shared/components/cards';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { DateLabelPipe, VideoDurationPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-vlog-detail',
  imports: [RouterLink, PageHeroComponent, VlogCardComponent, IconComponent, DateLabelPipe, VideoDurationPipe, RevealDirective],
  templateUrl: './vlog-detail.component.html',
  styleUrl: './vlog-detail.component.scss',
})
export class VlogDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);

  protected readonly vlog = signal<Vlog | null>(null);
  protected readonly related = signal<Vlog[]>([]);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly playing = signal(false);
  protected readonly copied = signal(false);

  protected readonly crumbs = computed(() => [
    { label: 'Home', url: '/' },
    { label: 'Vlogs', url: '/vlogs' },
    { label: this.vlog()?.title ?? 'Film' },
  ]);

  async ngOnInit(): Promise<void> {
    await this.load(this.route.snapshot.paramMap.get('slug') ?? '');
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    try {
      const vlog = await this.api.get<Vlog>(`content/vlogs/${slug}`);
      this.vlog.set(vlog);
      this.site.setSeo(vlog.title, vlog.description ?? undefined);
      this.analytics.track('vlog_open', { metadata: { vlogId: vlog.id, title: vlog.title } });
      this.api.post(`content/vlogs/${slug}/view`).catch(() => undefined);
      void this.loadRelated(vlog);
    } catch (error) {
      this.vlog.set(null);
      this.notFound.set(true);
      if (toApiError(error).status !== 404) {
        this.toast.error(toApiError(error).message, 'Could not open the film');
      }
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRelated(vlog: Vlog): Promise<void> {
    try {
      const result = await this.api.get<Paged<Vlog>>('content/vlogs', { pageSize: 5 });
      this.related.set(result.items.filter((item) => item.id !== vlog.id).slice(0, 4));
    } catch {
      this.related.set([]);
    }
  }

  protected play(): void {
    this.playing.set(true);
    this.analytics.track('vlog_play', { metadata: { vlogId: this.vlog()?.id, title: this.vlog()?.title } });
  }

  protected async share(): Promise<void> {
    const vlog = this.vlog();
    if (!vlog) return;
    const url = `${location.origin}/vlogs/${vlog.slug}`;
    if (navigator.share) {
      try {
        await navigator.share({ title: vlog.title, text: vlog.description ?? '', url });
        return;
      } catch {
        // Fall through to copying the link.
      }
    }
    try {
      await navigator.clipboard.writeText(url);
      this.copied.set(true);
      this.toast.success('Link copied. Share the artistry.', 'Film link');
      setTimeout(() => this.copied.set(false), 2200);
    } catch {
      this.toast.info(url, 'Film link');
    }
  }
}
