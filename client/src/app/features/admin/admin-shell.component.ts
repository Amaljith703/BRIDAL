import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';

interface AdminNavItem { label: string; path: string; exact?: boolean; adminOnly?: boolean; }

@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
})
export class AdminShellComponent {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly navItems: AdminNavItem[] = [
    { label: 'Overview', path: '/admin', exact: true },
    { label: 'Analytics', path: '/admin/analytics' },
    { label: 'Orders', path: '/admin/orders' },
    { label: 'Appointments', path: '/admin/bookings' },
    { label: 'Customers', path: '/admin/customers' },
    { label: 'Reviews', path: '/admin/reviews' },
    { label: 'Enquiries', path: '/admin/contacts' },
    { label: 'Support desk', path: '/admin/tickets' },
    { label: 'Products', path: '/admin/catalog/products' },
    { label: 'Services', path: '/admin/catalog/services' },
    { label: 'Packages', path: '/admin/catalog/packages' },
    { label: 'Combos & offers', path: '/admin/catalog/combos' },
    { label: 'Pages & sections', path: '/admin/content/pages', adminOnly: true },
    { label: 'Hero slides', path: '/admin/content/hero-slides', adminOnly: true },
    { label: 'Banners', path: '/admin/content/banners', adminOnly: true },
    { label: 'Testimonials', path: '/admin/content/testimonials', adminOnly: true },
    { label: 'FAQs', path: '/admin/content/faqs', adminOnly: true },
    { label: 'Portfolio', path: '/admin/content/portfolio', adminOnly: true },
    { label: 'Video journal', path: '/admin/content/vlogs', adminOnly: true },
    { label: 'Social & music', path: '/admin/content/social-links', adminOnly: true },
    { label: 'Email templates', path: '/admin/content/email-templates', adminOnly: true },
    { label: 'Site settings', path: '/admin/content/settings', adminOnly: true },
    { label: 'Media library', path: '/admin/media', adminOnly: true },
    { label: 'Backups', path: '/admin/backups', adminOnly: true },
    { label: 'Audit log', path: '/admin/audit', adminOnly: true },
    { label: 'Studio assistant', path: '/admin/assistant' },
  ];

  protected visibleItems(): AdminNavItem[] {
    return this.navItems.filter((item) => !item.adminOnly || this.auth.isAdmin());
  }

  protected async signOut(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/']);
  }
}
