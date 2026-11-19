import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { Booking, OrderSummary, Paged } from '../../core/models';
import { DateLabelPipe, InrPipe } from '../../shared/pipes';

@Component({
  selector: 'app-account-home',
  imports: [RouterLink, DateLabelPipe, InrPipe],
  template: `
    <section class="account-home">
      @if (error()) { <div class="notice--error" role="alert">{{ error() }}</div> }
      @if (loading()) { <p class="muted" role="status">Loading your studio details…</p> }
      @else {
        <div class="account-stats">
          <a routerLink="/account/orders"><span>Orders</span><strong>{{ orders().totalCount }}</strong><small>View your purchases</small></a>
          <a routerLink="/account/bookings"><span>Appointments</span><strong>{{ bookings().length }}</strong><small>See studio visits</small></a>
          <a routerLink="/account/addresses"><span>Saved addresses</span><strong>{{ addressCount() }}</strong><small>Ready for checkout</small></a>
        </div>
        <section class="panel"><header class="panel-head"><div><h2>Recent orders</h2><p>Your latest notes from the studio</p></div><a routerLink="/account/orders">All orders →</a></header>
          @if (orders().items.length) { <div class="order-list">@for (order of orders().items; track order.id) {
            <a class="order-row" [routerLink]="['/account/orders', order.id]"><span><strong>{{ order.orderNumber }}</strong><small>{{ order.createdAtUtc | dateLabel }} · {{ order.itemCount }} items</small></span><span class="order-total"><strong>{{ order.totalAmount | inr }}</strong><small>{{ order.status }}</small></span></a>
          }</div> } @else { <p class="empty-note">When you place an order, it will be easy to find it here.</p><a routerLink="/products" class="btn btn--ghost">Explore the studio edit</a> }
        </section>
        <section class="panel"><header class="panel-head"><div><h2>Upcoming appointments</h2><p>A little time set aside for you</p></div><a routerLink="/account/bookings">All appointments →</a></header>
          @if (upcoming().length) { <div class="appointment-list">@for (booking of upcoming(); track booking.id) { <article class="appointment-row"><span class="date-chip"><strong>{{ booking.preferredDate | dateLabel }}</strong><small>{{ booking.preferredTime }}</small></span><span><strong>{{ booking.packageName || booking.serviceName || 'Studio appointment' }}</strong><small>{{ booking.bookingNumber }} · {{ booking.status }}</small></span></article> }</div> }
          @else { <p class="empty-note">There are no upcoming appointments just now.</p><a routerLink="/booking" class="btn btn--ghost">Book an appointment</a> }
        </section>
      }
    </section>
  `,
  styles: `
    :host{display:block}.account-home{display:grid;gap:16px;color:#3d2a2f}.account-stats{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px}.account-stats a,.panel{padding:20px;border:1px solid #eadfd9;border-radius:16px;background:#fffdfb}.account-stats a{display:grid;gap:7px;color:inherit;text-decoration:none}.account-stats a:hover{border-color:#c89ca4}.account-stats span,.account-stats small,.panel-head p,.order-row small,.appointment-row small,.date-chip small{color:#806e72;font-size:.77rem}.account-stats strong{font:500 1.65rem Georgia,serif}.panel-head{display:flex;justify-content:space-between;align-items:flex-start;gap:12px;margin-bottom:12px}.panel-head h2{margin:0;font:500 1.2rem Georgia,serif}.panel-head p{margin:5px 0 0}.panel-head a{color:#8e4a5b;font-size:.78rem;font-weight:700;text-decoration:none}.order-list,.appointment-list{display:grid}.order-row,.appointment-row{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:14px 4px;border-top:1px solid #f0e8e3;color:inherit;text-decoration:none}.order-row>span,.order-total,.appointment-row>span:last-child,.date-chip{display:grid;gap:5px}.order-row:hover strong{color:#8e4a5b}.order-total{text-align:right}.date-chip{min-width:118px}.empty-note{color:#806e72;font-size:.86rem}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}@media(max-width:600px){.account-stats{grid-template-columns:1fr}.account-stats a{grid-template-columns:1fr auto;align-items:center}.account-stats small{grid-column:1/-1}.panel{padding:16px}.date-chip{min-width:95px}}
  `,
})
export class AccountHomeComponent implements OnInit {
  private readonly api = inject(ApiService);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly orders = signal<Paged<OrderSummary>>({ items: [], page: 1, pageSize: 5, totalCount: 0, totalPages: 0, hasNext: false, hasPrevious: false });
  protected readonly bookings = signal<Booking[]>([]);
  protected readonly addressCount = signal(0);
  protected readonly upcoming = signal<Booking[]>([]);

  ngOnInit(): void { void this.load(); }

  private async load(): Promise<void> {
    const [orders, bookings, addresses] = await Promise.allSettled([
      this.api.get<Paged<OrderSummary>>('account/orders', { page: 1, pageSize: 5 }),
      this.api.get<Booking[]>('account/bookings'),
      this.api.get<unknown[]>('account/addresses'),
    ]);
    if (orders.status === 'fulfilled') this.orders.set(orders.value);
    if (bookings.status === 'fulfilled') {
      this.bookings.set(bookings.value);
      this.upcoming.set(bookings.value.filter((booking) => booking.status === 'Pending' || booking.status === 'Confirmed'));
    }
    if (addresses.status === 'fulfilled') this.addressCount.set(addresses.value.length);
    const failure = [orders, bookings, addresses].find((result) => result.status === 'rejected');
    if (failure?.status === 'rejected') this.error.set(toApiError(failure.reason).message);
    this.loading.set(false);
  }
}
