import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { IconComponent } from '../icon.component';

@Component({
  selector: 'app-site-footer',
  imports: [RouterLink, IconComponent],
  template: `
    <footer class="footer">
      <div class="footer__cta">
        <div class="shell footer__cta-inner">
          <div>
            <span class="eyebrow eyebrow--light">The chair is yours</span>
            <h2>Let’s make something<br /><em>that feels like you.</em></h2>
          </div>
          <div class="footer__cta-actions">
            <a routerLink="/booking" class="btn btn--gold">Book an appointment</a>
            <a [href]="site.whatsAppUrl() || '/contact'" target="_blank" rel="noopener" class="btn btn--ghost-light">
              <app-icon name="whatsapp" [size]="17" /> Chat on WhatsApp
            </a>
          </div>
        </div>
      </div>

      <div class="footer__main">
        <div class="shell footer__grid">
          <div class="footer__brand">
            @if (site.settings().logoUrl) {
              <img [src]="site.settings().logoUrl" [alt]="site.settings().businessName" width="170" height="46" class="footer__logo" />
            } @else {
              <span class="footer__word">{{ site.settings().businessName }}</span>
            }
            <p>{{ site.settings().tagline }}</p>
            <div class="footer__socials">
              @for (social of site.socialLinks(); track social.id) {
                <a [href]="social.url" target="_blank" rel="noopener" [attr.aria-label]="social.platform">
                  <app-icon [name]="social.icon || social.platform.toLowerCase()" [size]="17" />
                </a>
              }
            </div>
            <div class="footer__rating">
              <span class="stars" aria-label="5 out of 5 stars">
                @for (star of [1, 2, 3, 4, 5]; track star) {
                  <app-icon name="star" [size]="13" />
                }
              </span>
              <span>Loved by 600+ clients across India</span>
            </div>
          </div>

          <nav class="footer__col" aria-label="Studio">
            <h3>Studio</h3>
            <a routerLink="/about">Our story</a>
            <a routerLink="/services">Services</a>
            <a routerLink="/packages">Packages</a>
            <a routerLink="/portfolio">Portfolio</a>
            <a routerLink="/vlogs">Vlogs</a>
            <a routerLink="/testimonials">Love notes</a>
          </nav>

          <nav class="footer__col" aria-label="Visit and shop">
            <h3>Visit & shop</h3>
            <a routerLink="/products">The studio edit</a>
            <a routerLink="/combos">Combos & offers</a>
            <a routerLink="/booking">Book a chair</a>
            <a routerLink="/contact">Contact & map</a>
            <a routerLink="/faq">FAQ</a>
            <a routerLink="/support">Customer support</a>
          </nav>

          <div class="footer__col footer__contact">
            <h3>Find us</h3>
            <p class="footer__address">
              {{ site.settings().addressLine1 }}<br />
              @if (site.settings().addressLine2) {
                {{ site.settings().addressLine2 }}<br />
              }
              {{ site.settings().city }}, {{ site.settings().state }} {{ site.settings().postalCode }}
            </p>
            <a [href]="'tel:' + site.settings().phone"><app-icon name="phone" [size]="14" /> {{ site.settings().phone }}</a>
            <a [href]="'mailto:' + site.settings().email"><app-icon name="mail" [size]="14" /> {{ site.settings().email }}</a>
            <p class="footer__hours"><app-icon name="clock" [size]="14" /> {{ site.settings().businessHours }}</p>
          </div>
        </div>
      </div>

      <div class="footer__bar">
        <div class="shell footer__bar-inner">
          <span>© {{ year }} {{ site.settings().businessName }}. All rights reserved.</span>
          <span class="footer__bar-links">
            <a routerLink="/faq">Shipping & returns</a>
            <a routerLink="/faq">Privacy</a>
            <a routerLink="/faq">Terms</a>
            <a routerLink="/login" class="footer__admin">Studio login</a>
          </span>
          <span class="footer__payments">
            <span class="payment-chip">UPI</span>
            <span class="payment-chip">VISA</span>
            <span class="payment-chip">MC</span>
            <span class="payment-chip">Rupay</span>
          </span>
        </div>
        @if (site.settings().footerNote) {
          <p class="footer__note">{{ site.settings().footerNote }}</p>
        }
      </div>
    </footer>
  `,
  styles: `
    .footer {
      background: #24171b;
      color: rgba(250, 246, 241, 0.72);
    }
    .footer__cta {
      background:
        radial-gradient(900px 380px at 85% 0%, rgba(233, 200, 193, 0.15), transparent 60%),
        linear-gradient(120deg, #5e2e3c, #3a2028 65%, #24171b);
      border-bottom: 1px solid rgba(250, 246, 241, 0.08);
    }
    .footer__cta-inner {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
      gap: 32px;
      padding-block: clamp(44px, 6vw, 72px);
    }
    .footer__cta h2 {
      color: #faf6f1;
      font-size: clamp(2rem, 4vw, 3.5rem);
      margin-top: 14px;
    }
    .footer__cta h2 em {
      color: #e9c8c1;
      font-weight: 400;
    }
    .footer__cta-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      padding-bottom: 6px;
    }
    .footer__main {
      padding-block: clamp(42px, 5vw, 68px);
    }
    .footer__grid {
      display: grid;
      grid-template-columns: minmax(0, 1.5fr) repeat(2, minmax(0, 0.7fr)) minmax(0, 1.15fr);
      gap: clamp(28px, 5vw, 70px);
    }
    .footer__logo {
      width: 170px;
      height: auto;
      filter: brightness(0) invert(1);
      opacity: 0.92;
      margin-bottom: 14px;
    }
    .footer__word {
      display: block;
      font-family: var(--font-display);
      font-size: 1.7rem;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      color: #faf6f1;
      margin-bottom: 14px;
    }
    .footer__brand p {
      font-size: 0.86rem;
      line-height: 1.7;
      max-width: 34ch;
      color: rgba(250, 246, 241, 0.55);
    }
    .footer__socials {
      display: flex;
      gap: 7px;
      margin-top: 18px;
    }
    .footer__socials a {
      display: grid;
      place-items: center;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      border: 1px solid rgba(250, 246, 241, 0.14);
      color: rgba(250, 246, 241, 0.68);
      transition: 0.25s var(--ease);
    }
    .footer__socials a:hover {
      border-color: #c9a26b;
      color: #c9a26b;
      transform: translateY(-2px);
    }
    .footer__rating {
      display: flex;
      align-items: center;
      gap: 9px;
      margin-top: 20px;
      font-size: 0.66rem;
      color: rgba(250, 246, 241, 0.45);
    }
    .footer__rating .stars {
      color: #c9a26b;
      display: inline-flex;
      gap: 1px;
    }
    .footer__col {
      display: flex;
      flex-direction: column;
      gap: 9px;
      font-size: 0.82rem;
    }
    .footer__col h3 {
      font-family: var(--font-body);
      font-size: 0.62rem;
      font-weight: 700;
      letter-spacing: 0.2em;
      text-transform: uppercase;
      color: #c9a26b;
      margin-bottom: 8px;
    }
    .footer__col a {
      color: rgba(250, 246, 241, 0.58);
      width: fit-content;
      transition: color 0.2s ease;
    }
    .footer__col a:hover {
      color: #e9c8c1;
    }
    .footer__contact a,
    .footer__contact p {
      display: flex;
      align-items: flex-start;
      gap: 9px;
      color: rgba(250, 246, 241, 0.58);
      margin: 0;
      line-height: 1.6;
    }
    .footer__contact a:hover {
      color: #e9c8c1;
    }
    .footer__address {
      display: block !important;
      margin-bottom: 4px !important;
    }
    .footer__hours {
      margin-top: 4px !important;
    }
    .footer__bar {
      border-top: 1px solid rgba(250, 246, 241, 0.09);
      padding-block: 18px;
    }
    .footer__bar-inner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
      font-size: 0.68rem;
      color: rgba(250, 246, 241, 0.38);
    }
    .footer__bar-links {
      display: flex;
      gap: 16px;
      flex-wrap: wrap;
    }
    .footer__bar-links a:hover {
      color: #e9c8c1;
    }
    .footer__admin {
      opacity: 0.7;
    }
    .footer__payments {
      display: flex;
      gap: 5px;
    }
    .payment-chip {
      padding: 3px 8px;
      border-radius: 4px;
      border: 1px solid rgba(250, 246, 241, 0.13);
      font-size: 0.55rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      color: rgba(250, 246, 241, 0.45);
    }
    .footer__note {
      text-align: center;
      margin: 14px 0 0;
      font-size: 0.64rem;
      font-family: var(--font-display);
      font-style: italic;
      color: rgba(250, 246, 241, 0.3);
    }
    @media (max-width: 980px) {
      .footer__cta-inner {
        flex-direction: column;
        align-items: flex-start;
      }
      .footer__grid {
        grid-template-columns: 1fr 1fr;
      }
      .footer__brand {
        grid-column: 1 / -1;
      }
    }
    @media (max-width: 560px) {
      .footer__grid {
        grid-template-columns: 1fr;
      }
      .footer__bar-inner {
        flex-direction: column;
        align-items: flex-start;
      }
    }
  `,
})
export class SiteFooterComponent {
  protected readonly site = inject(SiteService);
  protected readonly year = new Date().getFullYear();
}
