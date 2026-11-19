import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-account-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './account-layout.component.html',
  styleUrl: './account-layout.component.scss',
})
export class AccountLayoutComponent {
  protected readonly auth = inject(AuthService);
  protected readonly links = [
    { label: 'Overview', path: '/account', exact: true },
    { label: 'My orders', path: '/account/orders' },
    { label: 'Appointments', path: '/account/bookings' },
    { label: 'Addresses', path: '/account/addresses' },
    { label: 'Reviews', path: '/account/reviews' },
    { label: 'Support', path: '/account/support' },
    { label: 'Profile', path: '/account/profile' },
    { label: 'Security', path: '/account/security' },
  ];
}
