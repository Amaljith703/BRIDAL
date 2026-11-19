import { Routes } from '@angular/router';
import { authGuard, guestGuard, staffGuard } from './core/guards';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/public-layout.component').then((m) => m.PublicLayoutComponent),
    children: [
      {
        path: '',
        title: 'Makeup Art | Bridal, Editorial & Everyday Glam',
        loadComponent: () => import('./features/home/home-page.component').then((m) => m.HomePageComponent),
      },
      {
        path: 'services',
        title: 'Services | Makeup Art',
        loadComponent: () =>
          import('./features/services/services-page.component').then((m) => m.ServicesPageComponent),
      },
      {
        path: 'services/:slug',
        title: 'Service | Makeup Art',
        loadComponent: () =>
          import('./features/services/service-detail.component').then((m) => m.ServiceDetailComponent),
      },
      {
        path: 'packages',
        title: 'Packages | Makeup Art',
        loadComponent: () =>
          import('./features/packages/packages-page.component').then((m) => m.PackagesPageComponent),
      },
      {
        path: 'packages/:slug',
        title: 'Package | Makeup Art',
        loadComponent: () =>
          import('./features/packages/package-detail.component').then((m) => m.PackageDetailComponent),
      },
      {
        path: 'products',
        title: 'The Studio Edit | Makeup Art',
        loadComponent: () =>
          import('./features/products/products-page.component').then((m) => m.ProductsPageComponent),
      },
      {
        path: 'products/:slug',
        title: 'Product | Makeup Art',
        loadComponent: () =>
          import('./features/products/product-detail.component').then((m) => m.ProductDetailComponent),
      },
      {
        path: 'combos',
        title: 'Combos & Offers | Makeup Art',
        loadComponent: () => import('./features/combos/combos-page.component').then((m) => m.CombosPageComponent),
      },
      {
        path: 'offers',
        title: 'Offers | Makeup Art',
        loadComponent: () => import('./features/combos/offers-page.component').then((m) => m.OffersPageComponent),
      },
      {
        path: 'portfolio',
        title: 'Portfolio | Makeup Art',
        loadComponent: () =>
          import('./features/portfolio/portfolio-page.component').then((m) => m.PortfolioPageComponent),
      },
      {
        path: 'vlogs',
        title: 'Vlogs | Makeup Art',
        loadComponent: () => import('./features/vlogs/vlogs-page.component').then((m) => m.VlogsPageComponent),
      },
      {
        path: 'vlogs/:slug',
        title: 'Vlog | Makeup Art',
        loadComponent: () => import('./features/vlogs/vlog-detail.component').then((m) => m.VlogDetailComponent),
      },
      {
        path: 'testimonials',
        title: 'Love Notes | Makeup Art',
        loadComponent: () =>
          import('./features/testimonials/testimonials-page.component').then((m) => m.TestimonialsPageComponent),
      },
      {
        path: 'about',
        title: 'The Studio | Makeup Art',
        loadComponent: () => import('./features/about/about-page.component').then((m) => m.AboutPageComponent),
      },
      {
        path: 'faq',
        title: 'FAQ | Makeup Art',
        loadComponent: () => import('./features/faq/faq-page.component').then((m) => m.FaqPageComponent),
      },
      {
        path: 'contact',
        title: 'Contact & Map | Makeup Art',
        loadComponent: () => import('./features/contact/contact-page.component').then((m) => m.ContactPageComponent),
      },
      {
        path: 'booking',
        title: 'Book an Appointment | Makeup Art',
        loadComponent: () => import('./features/booking/booking-page.component').then((m) => m.BookingPageComponent),
      },
      {
        path: 'support',
        title: 'Customer Support | Makeup Art',
        loadComponent: () => import('./features/support/support-page.component').then((m) => m.SupportPageComponent),
      },
      {
        path: 'cart',
        title: 'Your Bag | Makeup Art',
        loadComponent: () => import('./features/cart/cart-page.component').then((m) => m.CartPageComponent),
      },
      {
        path: 'checkout',
        title: 'Checkout | Makeup Art',
        canActivate: [authGuard],
        loadComponent: () => import('./features/checkout/checkout-page.component').then((m) => m.CheckoutPageComponent),
      },
      {
        path: 'order/:id',
        title: 'Order Confirmed | Makeup Art',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/checkout/order-confirmation.component').then((m) => m.OrderConfirmationComponent),
      },
      {
        path: 'login',
        title: 'Sign In | Makeup Art',
        canActivate: [guestGuard],
        data: { authMode: 'login' },
        loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
      },
      {
        path: 'register',
        title: 'Create an Account | Makeup Art',
        canActivate: [guestGuard],
        data: { authMode: 'register' },
        loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
      },
      {
        path: 'forgot-password',
        title: 'Reset Password | Makeup Art',
        data: { authMode: 'forgot' },
        loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
      },
      {
        path: 'reset-password',
        title: 'Choose a New Password | Makeup Art',
        data: { authMode: 'reset' },
        loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
      },
      {
        path: 'confirm-email',
        title: 'Confirm Email | Makeup Art',
        data: { authMode: 'confirm' },
        loadComponent: () => import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
      },
      {
        path: 'account',
        title: 'My Account | Makeup Art',
        canActivate: [authGuard],
        loadComponent: () => import('./features/account/account-layout.component').then((m) => m.AccountLayoutComponent),
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/account/account-home.component').then((m) => m.AccountHomeComponent),
          },
          {
            path: 'orders',
            loadComponent: () =>
              import('./features/account/account-orders.component').then((m) => m.AccountOrdersComponent),
          },
          {
            path: 'orders/:id',
            loadComponent: () =>
              import('./features/account/account-order-detail.component').then((m) => m.AccountOrderDetailComponent),
          },
          {
            path: 'addresses',
            loadComponent: () =>
              import('./features/account/account-addresses.component').then((m) => m.AccountAddressesComponent),
          },
          {
            path: 'bookings',
            loadComponent: () =>
              import('./features/account/account-bookings.component').then((m) => m.AccountBookingsComponent),
          },
          {
            path: 'reviews',
            loadComponent: () =>
              import('./features/account/account-reviews.component').then((m) => m.AccountReviewsComponent),
          },
          {
            path: 'support',
            loadComponent: () =>
              import('./features/account/account-support.component').then((m) => m.AccountSupportComponent),
          },
          {
            path: 'profile',
            loadComponent: () =>
              import('./features/account/account-profile.component').then((m) => m.AccountProfileComponent),
          },
          {
            path: 'security',
            loadComponent: () =>
              import('./features/account/account-security.component').then((m) => m.AccountSecurityComponent),
          },
        ],
      },
      {
        path: '**',
        title: 'Not Found | Makeup Art',
        loadComponent: () => import('./features/not-found/not-found.component').then((m) => m.NotFoundComponent),
      },
    ],
  },
  {
    path: 'admin',
    title: 'Studio Desk | Makeup Art',
    canActivate: [staffGuard],
    loadComponent: () => import('./features/admin/admin-shell.component').then((m) => m.AdminShellComponent),
    children: [
      {
        path: '',
        title: 'Dashboard | Makeup Art',
        loadComponent: () =>
          import('./features/admin/dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent),
      },
      {
        path: 'analytics',
        title: 'Analytics | Makeup Art',
        loadComponent: () =>
          import('./features/admin/dashboard/admin-analytics.component').then((m) => m.AdminAnalyticsComponent),
      },
      {
        path: 'orders',
        title: 'Orders | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-orders.component').then((m) => m.AdminOrdersComponent),
      },
      {
        path: 'orders/:id',
        title: 'Order Detail | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-order-detail.component').then((m) => m.AdminOrderDetailComponent),
      },
      {
        path: 'bookings',
        title: 'Appointments | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-bookings.component').then((m) => m.AdminBookingsComponent),
      },
      {
        path: 'customers',
        title: 'Customers | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-customers.component').then((m) => m.AdminCustomersComponent),
      },
      {
        path: 'reviews',
        title: 'Review Moderation | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-reviews.component').then((m) => m.AdminReviewsComponent),
      },
      {
        path: 'contacts',
        title: 'Enquiries | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-contacts.component').then((m) => m.AdminContactsComponent),
      },
      {
        path: 'tickets',
        title: 'Support Desk | Makeup Art',
        loadComponent: () =>
          import('./features/admin/operations/admin-tickets.component').then((m) => m.AdminTicketsComponent),
      },
      {
        path: 'catalog/:resource',
        title: 'Catalog | Makeup Art',
        loadComponent: () =>
          import('./features/admin/catalog/admin-resource.component').then((m) => m.AdminResourceComponent),
      },
      {
        path: 'content/settings',
        title: 'Site Settings | Makeup Art',
        loadComponent: () =>
          import('./features/admin/content/admin-settings.component').then((m) => m.AdminSettingsComponent),
      },
      {
        path: 'content/pages',
        title: 'Pages & Sections | Makeup Art',
        loadComponent: () =>
          import('./features/admin/content/admin-pages.component').then((m) => m.AdminPagesComponent),
      },
      {
        path: 'content/:resource',
        title: 'Content | Makeup Art',
        loadComponent: () =>
          import('./features/admin/content/admin-content-resource.component').then(
            (m) => m.AdminContentResourceComponent,
          ),
      },
      {
        path: 'media',
        title: 'Media Library | Makeup Art',
        loadComponent: () => import('./features/admin/media/admin-media.component').then((m) => m.AdminMediaComponent),
      },
      {
        path: 'backups',
        title: 'Backups | Makeup Art',
        loadComponent: () =>
          import('./features/admin/system/admin-backups.component').then((m) => m.AdminBackupsComponent),
      },
      {
        path: 'audit',
        title: 'Audit Log | Makeup Art',
        loadComponent: () => import('./features/admin/system/admin-audit.component').then((m) => m.AdminAuditComponent),
      },
      {
        path: 'assistant',
        title: 'AI Studio Assistant | Makeup Art',
        loadComponent: () =>
          import('./features/admin/system/admin-assistant.component').then((m) => m.AdminAssistantComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
