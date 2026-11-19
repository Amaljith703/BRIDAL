import {
  Component,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsService } from '../../core/analytics.service';
import { HeroSlide, PortfolioItem, Testimonial, Vlog } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { HorizontalRailComponent } from '../../shared/components/horizontal-rail.component';
import { HeroSceneComponent } from '../../shared/components/hero-scene.component';
import { LightboxItem, MediaLightboxComponent } from '../../shared/components/media-lightbox.component';
import {
  PackageCardComponent,
  PortfolioCardComponent,
  ProductCardComponent,
  ServiceCardComponent,
  TestimonialCardComponent,
  VlogCardComponent,
} from '../../shared/components/cards';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { InrPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-home-page',
  imports: [
    RouterLink,
    HorizontalRailComponent,
    HeroSceneComponent,
    MediaLightboxComponent,
    ProductCardComponent,
    ServiceCardComponent,
    PackageCardComponent,
    TestimonialCardComponent,
    PortfolioCardComponent,
    VlogCardComponent,
    IconComponent,
    ImgFallbackDirective,
    InrPipe,
    RevealDirective,
  ],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.scss',
})
export class HomePageComponent implements OnDestroy {
  protected readonly site = inject(SiteService);
  private readonly analytics = inject(AnalyticsService);
  private readonly toast = inject(ToastService);

  protected readonly slides = computed<HeroSlide[]>(() => {
    const list = this.site.heroSlides();
    if (list.length) return list;
    return [
      {
        id: 'fallback',
        eyebrow: 'Bridal artistry · Bengaluru',
        title: 'The look you’ll remember.',
        subtitle:
          'Soft-focus skin, colour with intention, and a finish that feels like you — for the moments that deserve a little more.',
        modelImageUrl: '/media/seed/hero-1.jpg',
        backgroundImageUrl: '/media/seed/hero-bg-1.jpg',
        ctaLabel: 'Explore bridal',
        ctaUrl: '/packages',
        secondaryCtaLabel: 'View portfolio',
        secondaryCtaUrl: '/portfolio',
        accentColor: '#E9C8C1',
        secondaryColor: '#C9A26B',
        effectJson: null,
        sortOrder: 1,
        isActive: true,
      },
    ];
  });

  protected readonly active = signal(0);
  protected readonly current = computed(() => this.slides()[Math.min(this.active(), this.slides().length - 1)]);
  protected readonly lightbox = signal<LightboxItem | null>(null);
  protected readonly faqOpen = signal(0);

  protected readonly featuredServices = computed(() => this.site.featuredServices());
  protected readonly featuredProducts = computed(() => this.site.featuredProducts());
  protected readonly packages = computed(() => this.site.featuredPackages().slice(0, 3));
  protected readonly portfolio = computed(() => this.site.portfolio().slice(0, 6));
  protected readonly vlogs = computed(() => this.site.vlogs().slice(0, 6));
  protected readonly testimonials = computed(() => this.site.testimonials().slice(0, 8));
  protected readonly offers = computed(() => this.site.offers().slice(0, 3));
  protected readonly faqs = computed(() => this.site.faqs().slice(0, 6));

  protected readonly marqueeWords = [
    'Bridal artistry',
    'Soft glam',
    'Editorial',
    'Skin first',
    'Private lessons',
    'Studio rituals',
    'Everyday colour',
  ];

  protected readonly marqueeLoop = [...this.marqueeWords, ...this.marqueeWords].map((word, index) => ({
    key: index,
    word,
  }));

  private timer?: ReturnType<typeof setInterval>;

  constructor() {
    this.timer = setInterval(() => {
      if (typeof document !== 'undefined' && document.hidden) return;
      this.next();
    }, 7600);
    // Reset the timer when someone navigates slides by hand.
    this.active;
  }

  protected go(index: number): void {
    this.active.set((index + this.slides().length) % this.slides().length);
  }

  protected next(): void {
    this.go(this.active() + 1);
  }

  protected previous(): void {
    this.go(this.active() - 1);
  }

  protected heroLead(slide: HeroSlide): string {
    const words = slide.title.trim().split(/\s+/);
    if (words.length < 3) return slide.title;
    return words.slice(0, -1).join(' ');
  }

  protected heroTail(slide: HeroSlide): string {
    const words = slide.title.trim().split(/\s+/);
    if (words.length < 3) return '';
    return words[words.length - 1];
  }

  protected openPortfolio(item: PortfolioItem): void {
    this.analytics.track('portfolio_view', { metadata: { itemId: item.id, title: item.title } });
    this.lightbox.set({
      title: item.title,
      imageUrl: item.imageUrl,
      videoUrl: item.videoUrl,
      description: item.description,
      meta: item.category,
    });
  }

  protected openVlog(item: Vlog): void {
    this.analytics.track('vlog_play', { metadata: { vlogId: item.id, title: item.title } });
    this.lightbox.set({
      title: item.title,
      imageUrl: item.thumbnailUrl,
      videoUrl: item.videoUrl,
      description: item.description,
      meta: `${item.type} vlog · ${item.influencerName ?? 'Makeup Art Studio'}`,
    });
  }

  protected openTestimonial(item: Testimonial): void {
    this.lightbox.set({
      title: `${item.customerName} · in her words`,
      imageUrl: item.imageUrl,
      videoUrl: item.videoUrl,
      description: item.quote,
      meta: 'Client film',
    });
  }

  protected toggleFaq(index: number): void {
    this.faqOpen.update((open) => (open === index ? -1 : index));
  }

  protected async copyCode(code: string | null | undefined): Promise<void> {
    if (!code) return;
    try {
      await navigator.clipboard.writeText(code);
      this.toast.success(`Code ${code} copied. Apply it at checkout.`, 'Offer saved');
    } catch {
      this.toast.info(`Your code is ${code}.`, 'Offer code');
    }
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }
}
