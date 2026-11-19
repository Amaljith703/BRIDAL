import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { Paged, Testimonial } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { TestimonialCardComponent } from '../../shared/components/cards';
import { LightboxItem, MediaLightboxComponent } from '../../shared/components/media-lightbox.component';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-testimonials-page',
  imports: [RouterLink, PageHeroComponent, TestimonialCardComponent, MediaLightboxComponent, IconComponent, ImgFallbackDirective, RevealDirective],
  templateUrl: './testimonials-page.component.html',
  styleUrl: './testimonials-page.component.scss',
})
export class TestimonialsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly site = inject(SiteService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);

  protected readonly items = signal<Testimonial[]>([]);
  protected readonly loading = signal(true);
  protected readonly lightbox = signal<LightboxItem | null>(null);

  protected readonly videoStory = computed(() => this.items().find((item) => !!item.videoUrl) ?? null);
  protected readonly written = computed(() =>
    this.items().filter((item) => item.id !== this.videoStory()?.id),
  );
  protected readonly average = computed(() => {
    const list = this.items();
    if (!list.length) return 5;
    return list.reduce((total, item) => total + item.rating, 0) / list.length;
  });

  protected readonly stats = computed(() => [
    { value: '600+', label: 'Brides styled' },
    { value: this.items().length ? `${this.items().length}` : '312', label: 'Written love notes' },
    { value: this.average().toFixed(1), label: 'Average rating' },
    { value: '12 yrs', label: 'Of artistry' },
  ]);

  async ngOnInit(): Promise<void> {
    await this.load();
    this.analytics.track('testimonials_view');
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.api.get<Paged<Testimonial>>('content/testimonials', { page: 1, pageSize: 60 });
      this.items.set(result.items);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load love notes');
      this.items.set(this.site.testimonials());
    } finally {
      this.loading.set(false);
    }
  }

  protected play(item: Testimonial): void {
    this.analytics.track('testimonial_play', { metadata: { testimonialId: item.id, name: item.customerName } });
    this.lightbox.set({
      title: `${item.customerName} · in her words`,
      imageUrl: item.imageUrl,
      videoUrl: item.videoUrl,
      description: item.quote,
      meta: item.role || 'Client film',
    });
  }
}
