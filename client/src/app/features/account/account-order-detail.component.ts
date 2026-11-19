import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { Address, Order } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { DateLabelPipe, InrPipe } from '../../shared/pipes';

@Component({
  selector: 'app-account-order-detail',
  imports: [RouterLink, DateLabelPipe, InrPipe],
  template: `
    <section class="account-page">
      <a routerLink="/account/orders" class="back-link">← All orders</a>
      @if (error()) { <div class="notice--error" role="alert">{{ error() }}</div> }
      @if (loading()) { <p class="muted" role="status">Loading order details…</p> }
      @else if (order(); as item) {
        <header class="order-head"><div><p class="eyebrow">Order {{ item.orderNumber }}</p><h2>Your order, <em>in detail.</em></h2><p>Placed {{ item.createdAtUtc | dateLabel }} · {{ item.status }}</p></div>
          @if (item.invoiceNumber) { <button type="button" class="btn btn--ghost" (click)="openInvoice()">View tax invoice</button> }
        </header>
        <section class="panel"><h3>Items</h3>@for (line of item.items; track line.id) { <article class="item-row"><span><strong>{{ line.name }}</strong><small>{{ line.variantName || line.sku || line.itemType }} · Qty {{ line.quantity }}</small></span><strong>{{ line.lineTotal | inr }}</strong></article> }</section>
        <div class="detail-grid"><section class="panel"><h3>Order total</h3><dl><div><dt>Subtotal</dt><dd>{{ item.subTotal | inr }}</dd></div><div><dt>Discount</dt><dd>−{{ item.discountAmount | inr }}</dd></div><div><dt>Shipping</dt><dd>{{ item.shippingAmount | inr }}</dd></div><div><dt>GST</dt><dd>{{ item.taxAmount | inr }}</dd></div><div class="grand"><dt>Total</dt><dd>{{ item.totalAmount | inr }}</dd></div></dl><p class="small-note">Payment: {{ item.paymentStatus }} · {{ item.payments[0]?.method || item.payments[0]?.provider || '—' }}</p></section>
          <section class="panel"><h3>Delivery details</h3><p><strong>{{ shippingAddress().fullName || item.customerName }}</strong></p><p>{{ shippingAddress().addressLine1 }}</p><p>{{ shippingAddress().addressLine2 }}</p><p>{{ shippingAddress().city }} {{ shippingAddress().postalCode }}</p><p>{{ shippingAddress().state }}, {{ shippingAddress().country }}</p><p>{{ item.customerPhone }}</p>@if(item.trackingNumber){<p class="tracking">Tracking: {{ item.trackingNumber }}</p>}</section></div>
        <section class="panel"><h3>Order updates</h3><ol class="timeline">@for(event of item.statusHistory; track event.changedAtUtc){<li><span class="timeline-dot"></span><div><strong>{{ event.status }}</strong><small>{{ event.changedAtUtc | dateLabel:true }}</small>@if(event.note){<p>{{ event.note }}</p>}</div></li>}</ol></section>
      }
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:14px}.back-link{color:#8e4a5b;font-size:.8rem;font-weight:700;text-decoration:none}.order-head{display:flex;justify-content:space-between;align-items:flex-end;gap:16px}.order-head h2{margin:7px 0;font:500 1.7rem Georgia,serif}.order-head h2 em{color:#8e4a5b;font-weight:400}.order-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.8rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.panel{padding:18px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.panel h3{margin:0 0 14px;font:500 1.12rem Georgia,serif}.item-row{display:flex;justify-content:space-between;gap:12px;padding:13px 0;border-top:1px solid #f0e8e3}.item-row span{display:grid;gap:5px}.item-row small,.small-note{color:#806e72;font-size:.76rem}.detail-grid{display:grid;grid-template-columns:1fr 1fr;gap:14px}dl{display:grid;gap:10px;margin:0}dl>div{display:flex;justify-content:space-between;gap:12px;font-size:.83rem}dt{color:#806e72}dd{margin:0}.grand{padding-top:10px;border-top:1px solid #eadfd9;font-weight:700}.small-note{margin:15px 0 0}.panel>p{margin:5px 0;color:#705d61;font-size:.84rem}.tracking{color:#8e4a5b!important;font-weight:700}.timeline{display:grid;gap:0;margin:0;padding:0;list-style:none}.timeline li{position:relative;display:flex;gap:11px;padding:0 0 18px}.timeline li:not(:last-child)::before{position:absolute;top:11px;bottom:0;left:5px;width:1px;background:#eadfd9;content:''}.timeline-dot{z-index:1;width:11px;height:11px;flex:0 0 auto;border:2px solid #8e4a5b;border-radius:50%;background:#fff}.timeline li div{display:grid;gap:4px}.timeline small{color:#806e72;font-size:.72rem}.timeline p{margin:2px 0;color:#705d61;font-size:.8rem}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}@media(max-width:560px){.order-head{align-items:flex-start;flex-direction:column}.detail-grid{grid-template-columns:1fr}}
  `,
})
export class AccountOrderDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  protected readonly order = signal<Order | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly shippingAddress = computed<Partial<Address>>(() => {
    try { return JSON.parse(this.order()?.shippingAddressJson ?? '{}') as Partial<Address>; }
    catch { return {}; }
  });

  ngOnInit(): void { void this.load(); }

  protected async openInvoice(): Promise<void> {
    const item = this.order();
    if (!item) return;
    try {
      const blob = await this.api.blob(`account/orders/${item.id}/invoice`);
      const url = URL.createObjectURL(blob);
      const windowRef = window.open(url, '_blank', 'noopener');
      if (!windowRef) this.toast.warning('Allow pop-ups to view your invoice.', 'Invoice ready');
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (error) { this.toast.error(toApiError(error).message, 'Could not open the invoice'); }
  }

  private async load(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.error.set('The order could not be found.'); this.loading.set(false); return; }
    try { this.order.set(await this.api.get<Order>(`account/orders/${id}`)); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.loading.set(false); }
  }
}
