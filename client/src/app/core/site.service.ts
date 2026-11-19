import { Injectable, computed, inject, signal } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { ApiService } from './api.service';
import {
  Banner,
  Bootstrap,
  Category,
  ContentPage,
  Faq,
  HeroSlide,
  MusicTrack,
  Offer,
  PortfolioItem,
  Product,
  Service,
  ServicePackage,
  SiteSettings,
  SocialLink,
  Testimonial,
  Vlog,
} from './models';
import { ToastService } from './toast.service';

export const DEFAULT_SETTINGS: SiteSettings = {
  id: '',
  businessName: 'Makeup Art',
  tagline: 'Artistry that still looks like you.',
  logoUrl: '/media/seed/logo.svg',
  faviconUrl: '/media/seed/favicon.svg',
  email: 'hello@makeupart.local',
  phone: '+91 98765 43210',
  whatsApp: '+919876543210',
  addressLine1: '12 Rose Atelier, 100 Feet Road',
  addressLine2: 'Indiranagar',
  city: 'Bengaluru',
  state: 'Karnataka',
  postalCode: '560038',
  country: 'India',
  mapLatitude: 12.9716,
  mapLongitude: 77.5946,
  mapZoom: 14,
  googleMapsEmbedUrl: null,
  businessHours: 'Mon–Sat · 9:00 AM – 8:00 PM · Sunday by appointment',
  currency: 'INR',
  primaryColor: '#8E4A5B',
  secondaryColor: '#C9A26B',
  accentColor: '#E9C8C1',
  surfaceColor: '#FAF6F1',
  announcementText: 'Complimentary skin consult with every bridal booking.',
  seoTitle: 'Makeup Art | Bridal, Editorial & Everyday Glam',
  seoDescription: 'A boutique makeup studio in Bengaluru for bridal artistry, editorial looks, lessons and curated beauty essentials.',
  musicEnabled: true,
  defaultMusicTrackId: null,
  musicVolume: 32,
  gstEnabled: true,
  pricesIncludeTax: true,
  gstNumber: '29ABCDE1234F1Z5',
  businessState: 'Karnataka',
  instagramUrl: null,
  facebookUrl: null,
  youtubeUrl: null,
  pinterestUrl: null,
  tiktokUrl: null,
  xUrl: null,
  maintenanceMode: false,
  supportEmail: 'care@makeupart.local',
  supportPhone: '+91 98765 43211',
  heroBadge: null,
  footerNote: 'Made with care, colour and a little bit of magic.',
};

const EMPTY_BOOTSTRAP: Bootstrap = {
  settings: DEFAULT_SETTINGS,
  socialLinks: [],
  music: [],
  categories: [],
  heroSlides: [],
  featuredServices: [],
  featuredProducts: [],
  featuredPackages: [],
  testimonials: [],
  faqs: [],
  portfolio: [],
  vlogs: [],
  offers: [],
  banners: [],
};

@Injectable({ providedIn: 'root' })
export class SiteService {
  private readonly api = inject(ApiService);
  private readonly meta = inject(Meta);
  private readonly toast = inject(ToastService);

  readonly data = signal<Bootstrap>(EMPTY_BOOTSTRAP);
  readonly loaded = signal(false);
  readonly loading = signal(false);
  readonly settings = computed(() => this.data().settings);
  readonly socialLinks = computed(() => this.data().socialLinks);
  readonly music = computed(() => this.data().music);
  readonly categories = computed(() => this.data().categories);
  readonly heroSlides = computed(() => this.data().heroSlides);
  readonly featuredServices = computed(() => this.data().featuredServices);
  readonly featuredProducts = computed(() => this.data().featuredProducts);
  readonly featuredPackages = computed(() => this.data().featuredPackages);
  readonly testimonials = computed(() => this.data().testimonials);
  readonly faqs = computed(() => this.data().faqs);
  readonly portfolio = computed(() => this.data().portfolio);
  readonly vlogs = computed(() => this.data().vlogs);
  readonly offers = computed(() => this.data().offers);
  readonly banners = computed(() => this.data().banners);
  readonly whatsAppUrl = computed(() => {
    const raw = this.settings().whatsApp?.replace(/\D/g, '') ?? '';
    return raw ? `https://wa.me/${raw}` : '';
  });
  readonly mapEmbedUrl = computed(() => {
    const settings = this.settings();
    if (settings.googleMapsEmbedUrl) return settings.googleMapsEmbedUrl;
    return `https://www.google.com/maps?q=${settings.mapLatitude},${settings.mapLongitude}&z=${settings.mapZoom}&output=embed`;
  });
  readonly directionsUrl = computed(() => {
    const settings = this.settings();
    return `https://www.google.com/maps/dir/?api=1&destination=${settings.mapLatitude},${settings.mapLongitude}`;
  });

  private loadPromise: Promise<Bootstrap> | null = null;
  private readonly pageCache = new Map<string, ContentPage>();

  load(force = false): Promise<Bootstrap> {
    if (this.loaded() && !force) return Promise.resolve(this.data());
    if (this.loadPromise && !force) return this.loadPromise;
    this.loading.set(true);
    this.loadPromise = this.api
      .get<Bootstrap>('content/bootstrap')
      .then((data) => {
        this.data.set(data);
        this.loaded.set(true);
        this.applyBranding(data.settings);
        return data;
      })
      .catch((error) => {
        this.loading.set(false);
        this.loadPromise = null;
        if (!this.loaded()) {
          this.toast.warning('Using the studio’s last known styling while we reconnect.', 'Offline styling');
        }
        throw error;
      })
      .finally(() => {
        this.loading.set(false);
        this.loadPromise = null;
      });
    return this.loadPromise;
  }

  async refresh(): Promise<void> {
    await this.load(true);
    this.pageCache.clear();
  }

  async page(key: string): Promise<ContentPage> {
    const cached = this.pageCache.get(key);
    if (cached) return cached;
    const page = await this.api.get<ContentPage>(`content/pages/${key}`);
    this.pageCache.set(key, page);
    return page;
  }

  applyBranding(settings: SiteSettings): void {
    const root = document.documentElement;
    if (settings.primaryColor) root.style.setProperty('--brand-primary', settings.primaryColor);
    if (settings.secondaryColor) root.style.setProperty('--brand-secondary', settings.secondaryColor);
    if (settings.accentColor) root.style.setProperty('--brand-accent', settings.accentColor);
    if (settings.surfaceColor) root.style.setProperty('--brand-surface', settings.surfaceColor);
    if (settings.faviconUrl) {
      let icon = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
      if (!icon) {
        icon = document.createElement('link');
        icon.rel = 'icon';
        document.head.appendChild(icon);
      }
      icon.href = settings.faviconUrl;
    }
    if (settings.seoTitle) document.title = settings.seoTitle;
    if (settings.seoDescription) this.meta.updateTag({ name: 'description', content: settings.seoDescription });
    this.meta.updateTag({ name: 'theme-color', content: settings.primaryColor || '#8E4A5B' });
    this.meta.updateTag({ property: 'og:site_name', content: settings.businessName });
  }

  setSeo(title?: string | null, description?: string | null): void {
    const settings = this.settings();
    document.title = title ? `${title} | ${settings.businessName}` : settings.seoTitle || settings.businessName;
    if (description) this.meta.updateTag({ name: 'description', content: description });
    else if (settings.seoDescription) this.meta.updateTag({ name: 'description', content: settings.seoDescription });
  }

  invalidatePage(key: string): void {
    this.pageCache.delete(key);
  }

  // Convenience selectors used by pages while data is still streaming in.
  findService(slug: string): Service | undefined {
    return this.data().featuredServices.find((item) => item.slug === slug);
  }

  findProduct(slug: string): Product | undefined {
    return this.data().featuredProducts.find((item) => item.slug === slug);
  }

  hero(): HeroSlide[] {
    return this.data().heroSlides;
  }

  testimonialsTop(count = 6): Testimonial[] {
    return this.data().testimonials.slice(0, count);
  }

  portfolioTop(count = 8): PortfolioItem[] {
    return this.data().portfolio.slice(0, count);
  }

  vlogsTop(count = 6): Vlog[] {
    return this.data().vlogs.slice(0, count);
  }

  activeOffers(): Offer[] {
    return this.data().offers;
  }

  homeBanners(): Banner[] {
    return this.data().banners.filter((banner) => banner.placement === 'home');
  }

  faqsFor(category: string): Faq[] {
    return this.data().faqs.filter((faq) => faq.category === category);
  }

  categoriesInUse(): Category[] {
    return this.data().categories;
  }

  packagesTop(count = 4): ServicePackage[] {
    return this.data().featuredPackages.slice(0, count);
  }
}
