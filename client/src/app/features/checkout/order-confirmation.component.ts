import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, downloadBlob, toApiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Order } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { DateLabelPipe, InrPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

interface AddressView {
  fullName: string;
  phone: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

@Component({
  selector: 'app-order-confirmation',
  imports: [RouterLink, IconComponent, ImgFallbackDirective, DateLabelPipe, InrPipe, RevealDirective],
  templateUrl: './order-confirmation.component.html',
  styleUrl: './order-confirmation.component.scss',
})
export class OrderConfirmationComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  protected readonly auth = inject(AuthService);
  protected readonly site = inject(SiteService);

  protected readonly order = signal<Order | null>(null);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly invoiceBusy = signal(false);
  protected readonly isNew = signal(false);

  protected readonly shippingAddress = computed(() => this.parseAddress(this.order()?.shippingAddressJson));
  protected readonly billingAddress = computed(() => this.parseAddress(this.order()?.billingAddressJson));
  protected readonly itemCount = computed(() =>
    (this.order()?.items ?? []).reduce((total, item) => total + item.quantity, 0),
  );
  protected readonly latestPayment = computed(() => this.order()?.payments?.[0] ?? null);
  protected readonly firstName = computed(() => this.order()?.customerName.split(' ')[0] ?? 'friend');
  protected readonly statusTone = computed(() => this.toneFor(this.order()?.status ?? ''));

  protected readonly nextSteps = [
    {
      icon: 'mail',
      title: 'Confirmation email',
      copy: 'A copy of this order and the GST invoice are emailed to you. Check spam the first time.',
    },
    {
      icon: 'truck',
      title: 'Packing & tracking',
      copy: 'Physical pieces leave the studio within 24 hours. Tracking appears here and in My Orders.',
    },
    {
      icon: 'calendar',
      title: 'Studio appointments',
      copy: 'Package and service orders are confirmed by an artist within one working day.',
    },
  ];

  ngOnInit(): void {
    this.isNew.set(this.route.snapshot.queryParamMap.get('new') === '1');
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.failed.set(true);
      this.loading.set(false);
      return;
    }
    void this.load(id);
  }

  private async load(id: string): Promise<void> {
    this.loading.set(true);
    try {
      const order = await this.api.get<Order>(`account/orders/${id}`);
      this.order.set(order);
      this.failed.set(false);
    } catch (error) {
      this.failed.set(true);
      this.toast.error(toApiError(error).message, 'Could not open the order');
    } finally {
      this.loading.set(false);
    }
  }

  protected async downloadInvoice(): Promise<void> {
    const order = this.order();
    if (!order || this.invoiceBusy()) return;
    this.invoiceBusy.set(true);
    try {
      const blob = await this.api.blob(`account/orders/${order.id}/invoice`);
      downloadBlob(blob, `invoice-${order.invoiceNumber || order.orderNumber}.html`);
      this.toast.success('Invoice saved. Open it in any browser to print or save as PDF.', 'Invoice downloaded');
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Invoice not available yet');
    } finally {
      this.invoiceBusy.set(false);
    }
  }

  protected copyOrderNumber(): void {
    const order = this.order();
    if (!order) return;
    void navigator.clipboard
      ?.writeText(order.orderNumber)
      .then(() => this.toast.success(`${order.orderNumber} copied.`, 'Copied'))
      .catch(() => this.toast.info(order.orderNumber, 'Your order number'));
  }

  protected toneFor(status: string): string {
    switch (status) {
      case 'Delivered':
      case 'Paid':
      case 'Processing':
      case 'Shipped':
      case 'Confirmed':
      case 'Completed':
        return 'success';
      case 'Pending':
      case 'PaymentPending':
        return 'warning';
      case 'Cancelled':
      case 'Failed':
        return 'danger';
      default:
        return 'info';
    }
  }

  private parseAddress(json?: string | null): AddressView | null {
    if (!json) return null;
    try {
      const parsed = JSON.parse(json) as Partial<AddressView>;
      if (!parsed || !parsed.addressLine1) return null;
      return {
        fullName: parsed.fullName ?? '',
        phone: parsed.phone ?? '',
        addressLine1: parsed.addressLine1,
        addressLine2: parsed.addressLine2 ?? null,
        city: parsed.city ?? '',
        state: parsed.state ?? '',
        postalCode: parsed.postalCode ?? '',
        country: parsed.country ?? 'India',
      };
    } catch {
      return null;
    }
  }
}
