import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../../core/api.service';
import { OrderStatus, OrderSummary, Paged } from '../../../core/models';
import { DateLabelPipe, InrPipe } from '../../../shared/pipes';

@Component({
  selector: 'app-admin-orders',
  imports: [FormsModule, RouterLink, DateLabelPipe, InrPipe],
  template: `
    <section class="admin-page">
      <header class="admin-page-head"><div><p class="eyebrow">Commerce</p><h1>Orders</h1><p>Find orders, check payment progress, and follow each parcel through the studio.</p></div><button class="admin-button" type="button" (click)="load()">Refresh</button></header>
      <div class="admin-toolbar"><label class="admin-search">Search orders<input [(ngModel)]="search" (keyup.enter)="page.set(1); load()" placeholder="Order number, name or email" /></label><label>Status<select [(ngModel)]="status" (change)="page.set(1); load()"><option value="">All statuses</option>@for (item of statuses; track item) { <option [value]="item">{{ item }}</option> }</select></label><button class="admin-button primary" type="button" (click)="page.set(1); load()">Apply</button></div>
      @if (error()) { <div class="admin-alert" role="alert">{{ error() }}</div> }
      <div class="admin-panel"><div class="admin-panel-head"><div><h2>Order book</h2><p>{{ result()?.totalCount ?? 0 }} orders</p></div></div>
        @if (loading()) { <p class="admin-empty">Loading orders…</p> }
        @else if (result()?.items?.length) { <div class="admin-table-wrap"><table class="admin-table"><thead><tr><th>Order</th><th>Customer</th><th>Placed</th><th>Payment</th><th>Status</th><th class="numeric">Total</th><th></th></tr></thead><tbody>@for (order of result()!.items; track order.id) { <tr><td><strong>{{ order.orderNumber }}</strong><small>{{ order.itemCount }} item{{ order.itemCount === 1 ? '' : 's' }}</small></td><td>{{ order.customerName }}<small>{{ order.customerEmail }}</small></td><td>{{ order.createdAtUtc | dateLabel }}</td><td><span class="admin-pill">{{ order.paymentStatus }}</span></td><td><span class="admin-pill" [class.attention]="order.status === 'Pending' || order.status === 'PaymentPending'">{{ order.status }}</span></td><td class="numeric">{{ order.totalAmount | inr }}</td><td><a class="admin-link" [routerLink]="['/admin/orders', order.id]">Open</a></td></tr> }</tbody></table></div> }
        @else { <p class="admin-empty">No orders match these filters.</p> }
        <footer class="admin-pager"><span>Page {{ result()?.page ?? 1 }} of {{ result()?.totalPages ?? 1 }}</span><div><button class="admin-button" [disabled]="page() <= 1 || loading()" (click)="page.update(v => v - 1); load()">Previous</button><button class="admin-button" [disabled]="!result()?.hasNext || loading()" (click)="page.update(v => v + 1); load()">Next</button></div></footer>
      </div>
    </section>`,
})
export class AdminOrdersComponent {
  private readonly api = inject(ApiService);
  protected readonly statuses: OrderStatus[] = ['Pending', 'PaymentPending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Refunded', 'Failed'];
  protected readonly result = signal<Paged<OrderSummary> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected search = '';
  protected status = '';

  constructor() { void this.load(); }

  protected async load(): Promise<void> {
    this.loading.set(true); this.error.set(null);
    try { this.result.set(await this.api.get<Paged<OrderSummary>>('admin/orders', { page: this.page(), pageSize: 20, search: this.search, status: this.status })); }
    catch (e) { this.error.set(toApiError(e).message); }
    finally { this.loading.set(false); }
  }
}
