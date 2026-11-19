import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { OrderSummary, Paged } from '../../core/models';
import { DateLabelPipe, InrPipe } from '../../shared/pipes';

@Component({
  selector: 'app-account-orders',
  imports: [FormsModule, RouterLink, DateLabelPipe, InrPipe],
  template: `
    <section class="account-page">
      <header class="page-head"><div><p class="eyebrow">Your history</p><h2>Orders & invoices</h2><p>Every purchase, together in one place.</p></div></header>
      <form class="search" (ngSubmit)="searchOrders()"><label class="sr-only" for="order-search">Search order numbers</label><input id="order-search" class="input" name="search" [(ngModel)]="searchText" placeholder="Search order number…" /><button class="btn btn--ghost" type="submit">Search</button></form>
      @if (error()) { <div class="notice--error" role="alert">{{ error() }}</div> }
      @if (loading()) { <p class="muted" role="status">Loading your orders…</p> }
      @else if (result().items.length) {
        <div class="order-list">@for (order of result().items; track order.id) {
          <article class="order-card"><div class="order-top"><div><a class="order-number" [routerLink]="[order.id]">{{ order.orderNumber }}</a><p>{{ order.createdAtUtc | dateLabel }} · {{ order.itemCount }} items</p></div><span class="status-pill">{{ order.status }}</span></div><div class="order-bottom"><span>{{ order.paymentStatus }} <small>· {{ order.currency }}</small></span><strong>{{ order.totalAmount | inr }}</strong><a [routerLink]="[order.id]" class="detail-link">View details →</a></div></article>
        }</div>
        <div class="pagination"><button type="button" class="btn btn--ghost" (click)="changePage(-1)" [disabled]="!result().hasPrevious">Previous</button><span>Page {{ result().page }} of {{ result().totalPages }}</span><button type="button" class="btn btn--ghost" (click)="changePage(1)" [disabled]="!result().hasNext">Next</button></div>
      } @else { <div class="empty-state"><h3>No orders found</h3><p>Once you place an order, it will appear here.</p><a routerLink="/products" class="btn btn--primary">Explore the studio edit</a></div> }
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:16px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.search{display:flex;gap:8px}.input{min-width:0;flex:1;padding:11px 13px;border:1px solid #ded0ca;border-radius:9px;background:#fffdfb}.order-list{display:grid;gap:10px}.order-card{padding:18px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.order-top,.order-bottom{display:flex;align-items:center;gap:14px}.order-top{justify-content:space-between}.order-number{color:#8e4a5b;font-weight:700;text-decoration:none}.order-top p{margin:5px 0 0;color:#806e72;font-size:.77rem}.status-pill{padding:6px 10px;border-radius:999px;background:#f5eeeb;font-size:.72rem}.order-bottom{padding-top:15px;margin-top:14px;border-top:1px solid #f0e8e3}.order-bottom>span{color:#705d61;font-size:.75rem}.order-bottom small{color:#806e72}.order-bottom>strong{margin-left:auto;font-size:.95rem}.detail-link{color:#8e4a5b;font-size:.78rem;font-weight:700;text-decoration:none}.pagination{display:flex;align-items:center;justify-content:center;gap:14px;color:#806e72;font-size:.78rem}.empty-state{padding:38px 18px;border:1px dashed #d9c8c2;border-radius:14px;text-align:center}.empty-state p{color:#806e72}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}@media(max-width:540px){.order-bottom{flex-wrap:wrap}.order-bottom>strong{margin-left:auto}.detail-link{width:100%;padding-top:5px}.order-card{padding:14px}}
  `,
})
export class AccountOrdersComponent implements OnInit {
  private readonly api = inject(ApiService);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly searchText = '';
  protected readonly query = signal('');
  protected readonly result = signal<Paged<OrderSummary>>({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasNext: false, hasPrevious: false });

  ngOnInit(): void { void this.load(); }

  protected searchOrders(): void { this.query.set(this.searchText.trim()); this.page.set(1); void this.load(); }
  protected changePage(delta: number): void { this.page.update((value) => Math.max(1, value + delta)); void this.load(); }

  private async load(): Promise<void> {
    this.loading.set(true); this.error.set(null);
    try { this.result.set(await this.api.get<Paged<OrderSummary>>('account/orders', { page: this.page(), pageSize: 10, search: this.query() })); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.loading.set(false); }
  }
}
