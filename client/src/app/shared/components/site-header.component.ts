import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import { MusicService } from '../../core/music.service';
import { ScrollModeService } from '../../core/scroll-mode.service';
import { SiteService } from '../../core/site.service';
import { initials } from '../../core/format';
import { IconComponent } from '../icon.component';

interface NavLink {
  label: string;
  path: string;
  exact?: boolean;
}

@Component({
  selector: 'app-site-header',
  imports: [RouterLink, RouterLinkActive, FormsModule, IconComponent],
  template: `
    <header class="site-header" [class.is-scrolled]="scrolled()" [class.has-announcement]="showAnnouncement()">
      @if (showAnnouncement()) {
        <div class="announcement">
          <div class="shell announcement__inner">
            <app-icon name="sparkles" [size]="13" />
            <span>{{ site.settings().announcementText }}</span>
            @if (site.settings().phone) {
              <a [href]="'tel:' + site.settings().phone">{{ site.settings().phone }}</a>
            }
            <button type="button" class="announcement__close" (click)="dismissAnnouncement()" aria-label="Dismiss announcement">
              <app-icon name="close" [size]="12" />
            </button>
          </div>
        </div>
      }

      <div class="header-bar">
        <div class="shell header-bar__inner">
          <a routerLink="/" class="brand" aria-label="Makeup Art home">
            @if (site.settings().logoUrl) {
              <img [src]="site.settings().logoUrl" [alt]="site.settings().businessName" width="180" height="48" />
            } @else {
              <span class="brand__word">{{ site.settings().businessName }}</span>
            }
          </a>

          <nav class="primary-nav" aria-label="Primary navigation">
            @for (link of links; track link.path) {
              <a
                [routerLink]="link.path"
                routerLinkActive="is-active"
                [routerLinkActiveOptions]="{ exact: !!link.exact }"
                >{{ link.label }}</a
              >
            }
          </nav>

          <div class="header-actions">
            <button type="button" class="icon-btn header-action" (click)="toggleSearch()" aria-label="Search the studio">
              <app-icon name="search" [size]="19" />
            </button>

            <button
              type="button"
              class="icon-btn header-action"
              (click)="scrollMode.toggle()"
              aria-label="Standard view"
              [attr.aria-pressed]="scrollMode.standard()"
              [title]="scrollMode.standard() ? 'Switch to horizontal browsing' : 'Switch to standard view'"
            >
              <app-icon name="grid" [size]="18" />
            </button>

            @if (music.enabled()) {
              <button
                type="button"
                class="icon-btn header-action header-action--music"
                [class.is-playing]="music.playing()"
                (click)="music.toggle()"
                [attr.aria-label]="music.playing() ? 'Pause studio music' : 'Play studio music'"
              >
                <app-icon [name]="music.playing() ? 'volume' : 'volume-x'" [size]="19" />
              </button>
            }

            <div class="account-wrap">
              <button
                type="button"
                class="icon-btn header-action"
                (click)="toggleAccount()"
                [attr.aria-label]="auth.isAuthenticated() ? 'Your account' : 'Sign in'"
              >
                @if (auth.user(); as user) {
                  <span class="avatar-chip">{{ initials(user.displayName) }}</span>
                } @else {
                  <app-icon name="user" [size]="19" />
                }
              </button>
              @if (accountOpen()) {
                <div class="account-menu">
                  @if (auth.user(); as user) {
                    <div class="account-menu__head">
                      <strong>{{ user.displayName }}</strong>
                      <span>{{ user.email }}</span>
                    </div>
                    <a routerLink="/account" (click)="accountOpen.set(false)"><app-icon name="user" [size]="15" /> My account</a>
                    <a routerLink="/account/orders" (click)="accountOpen.set(false)"><app-icon name="receipt" [size]="15" /> Orders & invoices</a>
                    <a routerLink="/account/bookings" (click)="accountOpen.set(false)"><app-icon name="calendar" [size]="15" /> Appointments</a>
                    @if (auth.isStaff()) {
                      <a routerLink="/admin" (click)="accountOpen.set(false)"><app-icon name="grid" [size]="15" /> Studio desk</a>
                    }
                    <button type="button" (click)="signOut()"><app-icon name="logout" [size]="15" /> Sign out</button>
                  } @else {
                    <p class="account-menu__note">Sign in to see orders, appointments and saved details.</p>
                    <a routerLink="/login" (click)="accountOpen.set(false)" class="btn btn--primary btn--sm btn--block">Sign in</a>
                    <a routerLink="/register" (click)="accountOpen.set(false)" class="account-menu__link">Create an account</a>
                  }
                </div>
              }
            </div>

            <a routerLink="/cart" class="icon-btn header-action cart-action" aria-label="Your bag">
              <app-icon name="bag" [size]="19" />
              @if (cart.count() > 0) {
                <span class="cart-count">{{ cart.count() }}</span>
              }
            </a>

            <a routerLink="/booking" class="btn btn--primary btn--sm header-book">Book a chair</a>

            <button
              type="button"
              class="icon-btn header-action menu-toggle"
              (click)="menuOpen.set(true)"
              aria-label="Open menu"
            >
              <app-icon name="menu" [size]="22" />
            </button>
          </div>
        </div>
      </div>

      @if (searchOpen()) {
        <div class="search-panel">
          <form class="shell search-form" (submit)="submitSearch($event)">
            <app-icon name="search" [size]="20" />
            <input
              type="search"
              name="q"
              placeholder="Search services, products, looks…"
              [(ngModel)]="searchTerm"
              autocomplete="off"
              aria-label="Search"
            />
            <button type="submit" class="btn btn--dark btn--sm">Search</button>
            <button type="button" class="icon-btn" (click)="searchOpen.set(false)" aria-label="Close search">
              <app-icon name="close" [size]="18" />
            </button>
          </form>
          <div class="shell search-suggestions">
            <span>Try</span>
            @for (term of suggestions; track term) {
              <button type="button" (click)="quickSearch(term)">{{ term }}</button>
            }
          </div>
        </div>
      }

      @if (menuOpen()) {
        <div class="drawer-backdrop" (click)="menuOpen.set(false)"></div>
        <aside class="drawer" role="dialog" aria-modal="true" aria-label="Mobile navigation">
          <header class="drawer__head">
            <span class="brand__word">{{ site.settings().businessName }}</span>
            <button type="button" class="icon-btn" (click)="menuOpen.set(false)" aria-label="Close menu">
              <app-icon name="close" [size]="22" />
            </button>
          </header>
          <nav class="drawer__nav">
            <a routerLink="/" (click)="menuOpen.set(false)">Home</a>
            @for (link of links; track link.path) {
              <a [routerLink]="link.path" (click)="menuOpen.set(false)">{{ link.label }}</a>
            }
            <a routerLink="/booking" (click)="menuOpen.set(false)">Book an appointment</a>
            <a routerLink="/support" (click)="menuOpen.set(false)">Customer support</a>
          </nav>
          <div class="drawer__actions">
            <button type="button" class="btn btn--ghost btn--block" (click)="scrollMode.toggle()">
              {{ scrollMode.standard() ? 'Use horizontal browsing' : 'Use standard view' }}
            </button>
            <a routerLink="/cart" class="btn btn--ghost btn--block" (click)="menuOpen.set(false)">
              <app-icon name="bag" [size]="16" /> Your bag ({{ cart.count() }})
            </a>
            @if (auth.isAuthenticated()) {
              <a [routerLink]="auth.isStaff() ? '/admin' : '/account'" class="btn btn--dark btn--block" (click)="menuOpen.set(false)">
                {{ auth.isStaff() ? 'Studio desk' : 'My account' }}
              </a>
            } @else {
              <a routerLink="/login" class="btn btn--dark btn--block" (click)="menuOpen.set(false)">Sign in</a>
            }
            <a routerLink="/booking" class="btn btn--primary btn--block" (click)="menuOpen.set(false)">Book a chair</a>
          </div>
          <div class="drawer__contact">
            <a [href]="'tel:' + site.settings().phone"><app-icon name="phone" [size]="15" /> {{ site.settings().phone }}</a>
            <a [href]="'mailto:' + site.settings().email"><app-icon name="mail" [size]="15" /> {{ site.settings().email }}</a>
            <span><app-icon name="map-pin" [size]="15" /> {{ site.settings().city }}, {{ site.settings().state }}</span>
          </div>
          <div class="drawer__socials">
            @for (social of site.socialLinks(); track social.id) {
              <a [href]="social.url" target="_blank" rel="noopener" [attr.aria-label]="social.platform">
                <app-icon [name]="social.icon || social.platform.toLowerCase()" [size]="18" />
              </a>
            }
          </div>
        </aside>
      }
    </header>
  `,
  styles: `
    .site-header {
      position: sticky;
      top: 0;
      z-index: 1200;
      background: rgba(250, 246, 241, 0.94);
      backdrop-filter: blur(16px);
      border-bottom: 1px solid transparent;
      transition: border-color 0.3s var(--ease), box-shadow 0.3s var(--ease), background 0.3s var(--ease);
    }
    .site-header.is-scrolled {
      border-bottom-color: rgba(61, 42, 47, 0.1);
      box-shadow: 0 18px 40px -36px rgba(61, 42, 47, 0.55);
      background: rgba(250, 246, 241, 0.97);
    }
    .announcement {
      background: #332226;
      color: rgba(250, 246, 241, 0.82);
      font-size: 0.72rem;
      letter-spacing: 0.02em;
    }
    .announcement__inner {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 12px;
      min-height: 38px;
      position: relative;
      padding-right: 36px;
    }
    .announcement__inner app-icon {
      color: #c9a26b;
    }
    .announcement__inner a {
      color: #e9c8c1;
      text-decoration: underline;
      text-underline-offset: 3px;
      white-space: nowrap;
    }
    .announcement__close {
      position: absolute;
      right: 0;
      top: 50%;
      transform: translateY(-50%);
      width: 26px;
      height: 26px;
      display: grid;
      place-items: center;
      color: rgba(250, 246, 241, 0.5);
      border-radius: 50%;
    }
    .announcement__close:hover {
      color: #fff;
      background: rgba(250, 246, 241, 0.08);
    }
    .header-bar__inner {
      display: flex;
      align-items: center;
      gap: clamp(12px, 2.2vw, 34px);
      min-height: var(--header-height);
    }
    .brand {
      display: inline-flex;
      align-items: center;
      flex-shrink: 0;
    }
    .brand img {
      width: clamp(128px, 13vw, 172px);
      height: auto;
      display: block;
    }
    .brand__word {
      font-family: var(--font-display);
      font-size: 1.5rem;
      letter-spacing: 0.16em;
      text-transform: uppercase;
      white-space: nowrap;
    }
    .primary-nav {
      display: flex;
      align-items: center;
      gap: clamp(14px, 1.8vw, 26px);
      margin-inline: auto;
    }
    .primary-nav a {
      position: relative;
      font-size: 0.72rem;
      font-weight: 600;
      letter-spacing: 0.11em;
      text-transform: uppercase;
      color: #6f5a60;
      padding-block: 10px;
      white-space: nowrap;
      transition: color 0.25s var(--ease);
    }
    .primary-nav a::after {
      content: '';
      position: absolute;
      left: 0;
      right: 0;
      bottom: 4px;
      height: 1px;
      background: #8e4a5b;
      transform: scaleX(0);
      transform-origin: left;
      transition: transform 0.35s var(--ease);
    }
    .primary-nav a:hover,
    .primary-nav a.is-active {
      color: #8e4a5b;
    }
    .primary-nav a.is-active::after,
    .primary-nav a:hover::after {
      transform: scaleX(1);
    }
    .header-actions {
      display: flex;
      align-items: center;
      gap: 2px;
      margin-left: auto;
    }
    .header-action {
      color: #4d3a40;
      position: relative;
    }
    .header-action:hover {
      color: #8e4a5b;
    }
    .header-action--music.is-playing {
      color: #8e4a5b;
      animation: musicGlow 2.4s ease-in-out infinite;
    }
    .avatar-chip {
      display: grid;
      place-items: center;
      width: 30px;
      height: 30px;
      border-radius: 50%;
      background: #8e4a5b;
      color: #fff;
      font-size: 0.62rem;
      font-weight: 700;
      letter-spacing: 0.04em;
    }
    .cart-action {
      margin-right: 4px;
    }
    .cart-count {
      position: absolute;
      top: 3px;
      right: 2px;
      min-width: 17px;
      height: 17px;
      padding: 0 4px;
      border-radius: 999px;
      background: #8e4a5b;
      color: #fff;
      font-size: 0.56rem;
      font-weight: 700;
      display: grid;
      place-items: center;
      border: 2px solid #faf6f1;
    }
    .header-book {
      margin-left: 6px;
    }
    .menu-toggle {
      display: none;
    }
    .account-wrap {
      position: relative;
    }
    .account-menu {
      position: absolute;
      right: 0;
      top: calc(100% + 12px);
      width: 248px;
      padding: 10px;
      border-radius: 16px;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.12);
      box-shadow: 0 26px 60px -28px rgba(49, 26, 32, 0.45);
      display: grid;
      gap: 2px;
      animation: menuRise 0.25s var(--ease);
      z-index: 5;
    }
    .account-menu__head {
      display: grid;
      gap: 2px;
      padding: 10px 10px 12px;
      border-bottom: 1px solid rgba(61, 42, 47, 0.08);
      margin-bottom: 6px;
    }
    .account-menu__head strong {
      font-size: 0.84rem;
    }
    .account-menu__head span {
      font-size: 0.72rem;
      color: #6f5a60;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .account-menu a,
    .account-menu button {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 9px 10px;
      border-radius: 9px;
      font-size: 0.8rem;
      color: #4d3a40;
      text-align: left;
    }
    .account-menu a:hover,
    .account-menu button:hover {
      background: rgba(142, 74, 91, 0.07);
      color: #8e4a5b;
    }
    .account-menu__note {
      margin: 4px 6px 10px;
      font-size: 0.76rem;
      line-height: 1.55;
      color: #6f5a60;
    }
    .account-menu__link {
      justify-content: center;
      color: #8e4a5b !important;
      font-size: 0.75rem !important;
    }
    .search-panel {
      position: absolute;
      inset-inline: 0;
      top: 100%;
      background: #fffdfb;
      border-bottom: 1px solid rgba(61, 42, 47, 0.12);
      box-shadow: 0 30px 50px -38px rgba(49, 26, 32, 0.5);
      padding: 18px 0 14px;
      animation: searchDrop 0.28s var(--ease);
    }
    .search-form {
      display: flex;
      align-items: center;
      gap: 12px;
      color: #6f5a60;
    }
    .search-form input {
      flex: 1;
      min-width: 0;
      border: 0;
      background: transparent;
      font-family: var(--font-display);
      font-size: clamp(1.3rem, 2.5vw, 2rem);
      color: #332226;
      padding: 8px 0;
      outline: none;
    }
    .search-form input::placeholder {
      color: rgba(111, 90, 96, 0.42);
    }
    .search-suggestions {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
      margin-top: 8px;
      font-size: 0.7rem;
      color: #6f5a60;
    }
    .search-suggestions > span {
      letter-spacing: 0.12em;
      text-transform: uppercase;
      font-size: 0.6rem;
      opacity: 0.7;
    }
    .search-suggestions button {
      padding: 4px 11px;
      border: 1px solid rgba(61, 42, 47, 0.14);
      border-radius: 999px;
      font-size: 0.68rem;
      color: #6f5a60;
    }
    .search-suggestions button:hover {
      border-color: #8e4a5b;
      color: #8e4a5b;
    }
    .drawer-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(30, 18, 22, 0.5);
      backdrop-filter: blur(4px);
      z-index: 1300;
    }
    .drawer {
      position: fixed;
      top: 0;
      right: 0;
      bottom: 0;
      width: min(410px, 92vw);
      background: #faf6f1;
      z-index: 1400;
      padding: 22px;
      display: flex;
      flex-direction: column;
      gap: 18px;
      overflow-y: auto;
      box-shadow: -30px 0 70px -30px rgba(20, 10, 14, 0.4);
      animation: drawerIn 0.35s var(--ease);
    }
    .drawer__head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-bottom: 14px;
      border-bottom: 1px solid rgba(61, 42, 47, 0.12);
    }
    .drawer__nav {
      display: grid;
    }
    .drawer__nav a {
      font-family: var(--font-display);
      font-size: 1.65rem;
      padding: 10px 0;
      border-bottom: 1px solid rgba(61, 42, 47, 0.07);
      transition: color 0.2s ease, padding-left 0.2s ease;
    }
    .drawer__nav a:hover {
      color: #8e4a5b;
      padding-left: 8px;
    }
    .drawer__actions {
      display: grid;
      gap: 8px;
    }
    .drawer__contact {
      display: grid;
      gap: 10px;
      padding-top: 16px;
      border-top: 1px solid rgba(61, 42, 47, 0.1);
      font-size: 0.8rem;
      color: #6f5a60;
    }
    .drawer__contact a,
    .drawer__contact span {
      display: flex;
      align-items: center;
      gap: 9px;
    }
    .drawer__contact a:hover {
      color: #8e4a5b;
    }
    .drawer__socials {
      display: flex;
      gap: 8px;
      margin-top: auto;
      padding-top: 16px;
    }
    .drawer__socials a {
      display: grid;
      place-items: center;
      width: 38px;
      height: 38px;
      border-radius: 50%;
      border: 1px solid rgba(61, 42, 47, 0.14);
      color: #6f5a60;
    }
    .drawer__socials a:hover {
      border-color: #8e4a5b;
      color: #8e4a5b;
    }
    @keyframes menuRise {
      from {
        opacity: 0;
        transform: translateY(-6px);
      }
    }
    @keyframes searchDrop {
      from {
        opacity: 0;
        transform: translateY(-8px);
      }
    }
    @keyframes drawerIn {
      from {
        transform: translateX(100%);
      }
    }
    @keyframes musicGlow {
      0%,
      100% {
        opacity: 0.75;
      }
      50% {
        opacity: 1;
      }
    }
    @media (max-width: 1180px) {
      .primary-nav {
        gap: 14px;
      }
      .header-book {
        display: none;
      }
    }
    @media (max-width: 980px) {
      .primary-nav {
        display: none;
      }
      .menu-toggle {
        display: grid;
      }
      .header-actions {
        gap: 0;
      }
      .header-action--music {
        display: none;
      }
      .brand img {
        width: 128px;
      }
      .account-menu {
        right: -60px;
      }
    }
    @media (max-width: 560px) {
      .header-action:not(.cart-action):not(.menu-toggle) {
        display: none;
      }
      .announcement__inner {
        justify-content: flex-start;
        font-size: 0.66rem;
        gap: 8px;
      }
      .announcement__inner a {
        display: none;
      }
      .search-form .btn {
        display: none;
      }
    }
  `,
})
export class SiteHeaderComponent {
  protected readonly site = inject(SiteService);
  protected readonly cart = inject(CartService);
  protected readonly auth = inject(AuthService);
  protected readonly music = inject(MusicService);
  protected readonly scrollMode = inject(ScrollModeService);
  private readonly router = inject(Router);

  protected readonly scrolled = signal(false);
  protected readonly searchOpen = signal(false);
  protected readonly menuOpen = signal(false);
  protected readonly accountOpen = signal(false);
  protected readonly announcementHidden = signal(false);
  protected readonly showAnnouncement = computed(
    () => !this.announcementHidden() && !!this.site.settings().announcementText,
  );
  protected readonly initials = initials;
  protected searchTerm = '';

  protected readonly links: NavLink[] = [
    { label: 'Services', path: '/services' },
    { label: 'Packages', path: '/packages' },
    { label: 'Shop', path: '/products' },
    { label: 'Portfolio', path: '/portfolio' },
    { label: 'Vlogs', path: '/vlogs' },
    { label: 'About', path: '/about' },
    { label: 'Contact', path: '/contact' },
  ];

  protected readonly suggestions = ['Bridal', 'Soft glam', 'Skincare', 'Lipstick', 'Lessons'];

  constructor() {
    effect(() => {
      const open = this.menuOpen();
      if (typeof document === 'undefined') return;
      document.body.classList.toggle('no-scroll', open);
    });

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.menuOpen.set(false);
        this.searchOpen.set(false);
        this.accountOpen.set(false);
      });
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled.set(window.scrollY > 12);
  }

  protected dismissAnnouncement(): void {
    this.announcementHidden.set(true);
  }

  protected toggleSearch(): void {
    this.searchOpen.update((open) => !open);
    this.accountOpen.set(false);
  }

  protected toggleAccount(): void {
    this.accountOpen.update((open) => !open);
    this.searchOpen.set(false);
  }

  protected submitSearch(event: Event): void {
    event.preventDefault();
    const term = this.searchTerm.trim();
    this.searchOpen.set(false);
    this.router.navigate(['/products'], { queryParams: term ? { search: term } : {} });
  }

  protected quickSearch(term: string): void {
    this.searchTerm = term;
    this.searchOpen.set(false);
    this.router.navigate(['/products'], { queryParams: { search: term } });
  }

  protected async signOut(): Promise<void> {
    this.accountOpen.set(false);
    await this.auth.logout();
    this.router.navigate(['/']);
  }
}
