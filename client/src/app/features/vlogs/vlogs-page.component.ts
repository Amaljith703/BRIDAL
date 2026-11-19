import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Paged, Vlog, VlogType } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { VlogCardComponent } from '../../shared/components/cards';
import { LightboxItem, MediaLightboxComponent } from '../../shared/components/media-lightbox.component';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { DateLabelPipe, VideoDurationPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-vlogs-page',
  imports: [RouterLink, PageHeroComponent, VlogCardComponent, MediaLightboxComponent, IconComponent, VideoDurationPipe, DateLabelPipe, RevealDirective],
  templateUrl: './vlogs-page.component.html',
  styleUrl: './vlogs-page.component.scss',
})
export class VlogsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly vlogs = signal<Vlog[]>([]);
  protected readonly loading = signal(true);
  protected readonly type = signal<'All' | VlogType>('All');
  protected readonly search = signal('');
  protected readonly total = signal(0);
  protected readonly lightbox = signal<LightboxItem | null>(null);
  protected readonly featured = computed(() => this.vlogs().find((item) => item.isFeatured) ?? this.vlogs()[0] ?? null);
  protected readonly grid = computed(() => this.vlogs().filter((item) => item.id !== this.featured()?.id));

  protected readonly types: Array<'All' | VlogType> = ['All', 'Makeup', 'Influencer', 'Studio'];

  protected readonly channels = [
    { icon: 'youtube', label: 'YouTube', copy: 'Full-length bridal films and lessons.', url: 'https://youtube.com' },
    { icon: 'instagram', label: 'Instagram', copy: 'Daily studio moments and reels.', url: 'https://instagram.com' },
    { icon: 'facebook', label: 'Facebook', copy: 'Live Q&As and offer announcements.', url: 'https://facebook.com' },
  ];

  async ngOnInit(): Promise<void> {
    await this.load();
    this.analytics.track('vlogs_view');
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<Vlog>>('content/vlogs', {
        page: 1,
        pageSize: 48,
        type: this.type() === 'All' ? undefined : this.type(),
        search: this.search() || undefined,
      });
      this.vlogs.set(result.items);
      this.total.set(result.totalCount);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load the vlog channel');
      this.vlogs.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected setType(value: 'All' | VlogType): void {
    this.type.set(value);
    void this.load();
  }

  protected applySearch(value: string): void {
    this.search.set(value.trim());
    void this.load();
  }

  protected open(item: Vlog): void {
    this.analytics.track('vlog_play', { metadata: { vlogId: item.id, title: item.title } });
    this.lightbox.set({
      title: item.title,
      imageUrl: item.thumbnailUrl,
      videoUrl: item.videoUrl,
      description: item.description,
      meta: `${item.type} vlog · ${item.influencerName ?? 'Makeup Art Studio'}`,
    });
  }

  protected watchPage(item: Vlog): void {
    this.analytics.track('vlog_open_detail', { metadata: { vlogId: item.id, title: item.title } });
  }
}
