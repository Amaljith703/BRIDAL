import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import {
  Address,
  CheckoutQuote,
  CheckoutResponse,
  Order,
  PaymentConfig,
  ShippingMethod,
} from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { InrPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

type PaymentChoice = 'Mock' | 'Razorpay';

interface RazorpaySuccess {
  razorpay_order_id: string;
  razorpay_payment_id: string;
  razorpay_signature: string;
}

interface RazorpayInstance {
  open(): void;
  on(event: string, callback: (response: unknown) => void): void;
}

declare global {
  interface Window {
    Razorpay?: new (options: Record<string, unknown>) => RazorpayInstance;
  }
}

@Component({
  selector: 'app-checkout-page',
  imports: [RouterLink, ReactiveFormsModule, IconComponent, ImgFallbackDirective, InrPipe, RevealDirective],
  templateUrl: './checkout-page.component.html',
  styleUrl: './checkout-page.component.scss',
})
export class CheckoutPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly cart = inject(CartService);
  protected readonly auth = inject(AuthService);
  protected readonly site = inject(SiteService);

  protected readonly addresses = signal<Address[]>([]);
  protected readonly shippingMethods = signal<ShippingMethod[]>([]);
  protected readonly paymentConfig = signal<PaymentConfig | null>(null);
  protected readonly quote = signal<CheckoutQuote | null>(null);
  protected readonly quoteLoading = signal(false);
  protected readonly placing = signal(false);
  protected readonly pendingOrder = signal<CheckoutResponse | null>(null);
  protected readonly addressMode = signal<'saved' | 'new'>('new');
  protected readonly selectedAddressId = signal<string | null>(null);
  protected readonly selectedShippingId = signal<string | null>(null);
  protected readonly selectedPayment = signal<PaymentChoice | null>(null);
  protected readonly mockMethod = signal<'card' | 'upi'>('card');
  protected readonly billingSame = signal(true);
  protected readonly wantsGst = signal(false);
  protected readonly couponMessage = signal<string | null>(null);
  protected readonly couponApplied = signal(false);
  protected readonly errors = signal<string[]>([]);

  protected readonly couponControl = new FormControl('', { nonNullable: true });
  protected readonly gstControl = new FormControl('', {
    nonNullable: true,
    validators: [
      Validators.pattern(/^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/),
    ],
  });
  protected readonly notesControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(1000)],
  });

  protected readonly shippingForm = this.buildAddressForm();
  protected readonly billingForm = this.buildAddressForm();

  protected readonly states = [
    'Andhra Pradesh',
    'Arunachal Pradesh',
    'Assam',
    'Bihar',
    'Chandigarh',
    'Chhattisgarh',
    'Delhi',
    'Goa',
    'Gujarat',
    'Haryana',
    'Himachal Pradesh',
    'Jammu & Kashmir',
    'Jharkhand',
    'Karnataka',
    'Kerala',
    'Madhya Pradesh',
    'Maharashtra',
    'Manipur',
    'Meghalaya',
    'Mizoram',
    'Nagaland',
    'Odisha',
    'Puducherry',
    'Punjab',
    'Rajasthan',
    'Sikkim',
    'Tamil Nadu',
    'Telangana',
    'Tripura',
    'Uttar Pradesh',
    'Uttarakhand',
    'West Bengal',
  ];

  protected readonly paymentOptions = computed(() => {
    const config = this.paymentConfig();
    if (!config) return [] as Array<{ value: PaymentChoice; label: string; copy: string; icon: string; tag?: string }>;
    const options: Array<{ value: PaymentChoice; label: string; copy: string; icon: string; tag?: string }> = [];
    if (config.demoMode) {
      options.push({
        value: 'Mock',
        label: 'Demo payment',
        copy: 'Simulated capture — for development and preview builds.',
        icon: 'shield',
        tag: 'Demo',
      });
    }
    if (config.razorpayEnabled) {
      options.push({
        value: 'Razorpay',
        label: 'Razorpay',
        copy: 'UPI, credit and debit cards, netbanking and wallets.',
        icon: 'razorpay',
      });
    }
    return options;
  });

  protected readonly selectedShipping = computed(
    () => this.shippingMethods().find((method) => method.id === this.selectedShippingId()) ?? null,
  );
  protected readonly selectedAddress = computed(
    () => this.addresses().find((address) => address.id === this.selectedAddressId()) ?? null,
  );
  protected readonly itemCount = computed(() => this.cart.count());
  protected readonly breakdown = computed(() => this.quote()?.breakdown ?? null);

  ngOnInit(): void {
    if (!this.cart.lines().length && !this.pendingOrder()) {
      this.toast.info('Your bag is empty — add something lovely first.', 'Nothing to check out');
      void this.router.navigate(['/cart']);
      return;
    }
    const user = this.auth.user();
    if (user) {
      const name = `${user.firstName} ${user.lastName}`.trim();
      this.shippingForm.patchValue({ fullName: name, phone: user.phone ?? '' });
      this.billingForm.patchValue({ fullName: name, phone: user.phone ?? '' });
    }
    void this.load();
    this.analytics.track('checkout_open');
  }

  private buildAddressForm(): FormGroup {
    return this.fb.nonNullable.group({
      label: ['Home'],
      fullName: ['', [Validators.required, Validators.maxLength(150)]],
      phone: ['', [Validators.required, Validators.maxLength(30)]],
      addressLine1: ['', [Validators.required, Validators.maxLength(250)]],
      addressLine2: ['', [Validators.maxLength(250)]],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      state: ['Karnataka', [Validators.required, Validators.maxLength(100)]],
      postalCode: ['', [Validators.required, Validators.maxLength(20)]],
      country: ['India', [Validators.required, Validators.maxLength(100)]],
    });
  }

  private async load(): Promise<void> {
    const [addresses, methods, config] = await Promise.allSettled([
      this.api.get<Address[]>('account/addresses'),
      this.api.get<ShippingMethod[]>('catalog/shipping-methods'),
      this.api.get<PaymentConfig>('payments/config'),
    ]);

    if (addresses.status === 'fulfilled') {
      this.addresses.set(addresses.value);
      const preferred = addresses.value.find((address) => address.isDefaultShipping) ?? addresses.value[0];
      if (preferred) {
        this.selectedAddressId.set(preferred.id);
        this.addressMode.set('saved');
      }
    }
    if (methods.status === 'fulfilled') {
      this.shippingMethods.set(methods.value.filter((method) => method.isActive));
      const preferred = methods.value.find((method) => method.isDefault) ?? methods.value[0];
      if (preferred) this.selectedShippingId.set(preferred.id);
    }
    if (config.status === 'fulfilled') {
      this.paymentConfig.set(config.value);
      const razorpay = this.paymentOptions().find((option) => option.value === 'Razorpay');
      if (razorpay && !config.value.demoMode) this.selectedPayment.set('Razorpay');
      else if (config.value.demoMode) this.selectedPayment.set('Mock');
      else this.selectedPayment.set(this.paymentOptions()[0]?.value ?? null);
    } else if (config.status === 'rejected') {
      this.selectedPayment.set(null);
    }

    await this.refreshQuote();
  }

  protected async refreshQuote(): Promise<void> {
    if (!this.cart.lines().length) return;
    this.quoteLoading.set(true);
    try {
      const quote = await this.api.post<CheckoutQuote>('checkout/quote', {
        items: this.cart.itemsForApi(),
        shippingMethodId: this.selectedShippingId(),
        couponCode: this.couponControl.value.trim() || null,
        placeOfSupply: this.placeOfSupply(),
      });
      this.quote.set(quote);
      if (!this.selectedShippingId() && quote.shippingMethodId) {
        this.selectedShippingId.set(quote.shippingMethodId);
      }
      this.couponApplied.set(quote.coupon.applied);
      this.couponMessage.set(quote.coupon.message ?? null);
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not price the bag');
    } finally {
      this.quoteLoading.set(false);
    }
  }

  private placeOfSupply(): string {
    if (this.addressMode() === 'saved') return this.selectedAddress()?.state ?? '';
    return this.shippingForm.controls['state'].value as string;
  }

  protected applyCoupon(): void {
    if (!this.couponControl.value.trim()) {
      this.couponMessage.set('Enter a coupon code first.');
      return;
    }
    this.couponMessage.set(null);
    void this.refreshQuote();
  }

  protected clearCoupon(): void {
    this.couponControl.setValue('');
    this.couponMessage.set(null);
    this.couponApplied.set(false);
    void this.refreshQuote();
  }

  protected chooseShipping(id: string): void {
    if (this.selectedShippingId() === id) return;
    this.selectedShippingId.set(id);
    void this.refreshQuote();
  }

  protected chooseSavedAddress(address: Address): void {
    this.addressMode.set('saved');
    this.selectedAddressId.set(address.id);
    void this.refreshQuote();
  }

  protected useNewAddress(): void {
    this.addressMode.set('new');
    this.selectedAddressId.set(null);
  }

  protected copyShippingToBilling(): void {
    this.billingForm.patchValue(this.shippingForm.getRawValue());
  }

  protected setWantsGst(value: boolean): void {
    this.wantsGst.set(value);
    if (!value) this.gstControl.setValue('');
  }

  protected isMock(): boolean {
    return this.selectedPayment() === 'Mock';
  }

  protected async placeOrder(): Promise<void> {
    this.errors.set([]);
    const paymentProvider = this.selectedPayment();
    if (!paymentProvider) {
      this.toast.error('Payments are temporarily unavailable. Please contact the studio for help.');
      return;
    }
    const shipping = this.resolveShippingAddress();
    if (!shipping) return;

    let billing = null;
    if (!this.billingSame()) {
      if (this.billingForm.invalid) {
        this.billingForm.markAllAsTouched();
        this.toast.warning('Complete the billing address or tick “same as shipping”.', 'Billing address needed');
        return;
      }
      billing = this.billingForm.getRawValue();
    }
    if (this.wantsGst() && this.gstControl.invalid) {
      this.gstControl.markAsTouched();
      this.toast.warning('That GSTIN does not look right. It is 15 characters, e.g. 29AABCM1234A1Z5.', 'Check the GSTIN');
      return;
    }
    this.placing.set(true);
    try {
      const response = await this.api.post<CheckoutResponse>('checkout', {
        items: this.cart.itemsForApi(),
        shippingAddress: shipping,
        billingAddress: billing,
        shippingMethodId: this.selectedShippingId(),
        couponCode: this.couponControl.value.trim() || null,
        gstNumber: this.wantsGst() ? this.gstControl.value.trim().toUpperCase() : null,
        notes: this.notesControl.value.trim() || null,
        paymentProvider,
      });
      this.pendingOrder.set(response);
      if (response.provider === 'Mock' || !response.requiresPayment) {
        await this.completeMockPayment(response);
      } else {
        await this.payWithRazorpay(response);
      }
    } catch (error) {
      const apiError = toApiError(error);
      this.errors.set(apiError.fields ? Object.values(apiError.fields).flat() : [apiError.message]);
      this.toast.error(apiError.message, 'Could not place the order');
      this.placing.set(false);
    }
  }

  private resolveShippingAddress(): {
    label: string;
    fullName: string;
    phone: string;
    addressLine1: string;
    addressLine2: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  } | null {
    if (this.addressMode() === 'saved') {
      const saved = this.selectedAddress();
      if (saved) {
        return {
          label: saved.label,
          fullName: saved.fullName,
          phone: saved.phone,
          addressLine1: saved.addressLine1,
          addressLine2: saved.addressLine2 ?? '',
          city: saved.city,
          state: saved.state,
          postalCode: saved.postalCode,
          country: saved.country,
        };
      }
      this.toast.warning('Choose a saved address or add a new one.', 'Where should we send it?');
      this.addressMode.set('new');
      return null;
    }
    if (this.shippingForm.invalid) {
      this.shippingForm.markAllAsTouched();
      this.toast.warning('Fill in the delivery address so we know where to send everything.', 'Address missing');
      this.scrollTo('checkout-address');
      return null;
    }
    return this.shippingForm.getRawValue();
  }

  private async completeMockPayment(response: CheckoutResponse): Promise<void> {
    try {
      const order = await this.api.post<Order>('payments/mock/complete', {
        orderId: response.orderId,
        method: this.mockMethod() === 'upi' ? 'upi' : 'card',
      });
      this.finish(order.id, order.orderNumber);
    } catch (error) {
      this.toast.error(
        `${toApiError(error).message} Your order is saved — retry the payment below.`,
        'Demo payment did not complete',
      );
      this.placing.set(false);
    }
  }

  private async payWithRazorpay(response: CheckoutResponse): Promise<void> {
    const loaded = await this.loadRazorpayScript();
    const key = response.providerKeyId || this.paymentConfig()?.razorpayKeyId;
    if (!loaded || !window.Razorpay || !key || !response.providerOrderId) {
      this.toast.error('The payment window could not open. Please try again in a moment.', 'Payment unavailable');
      this.placing.set(false);
      return;
    }
    const user = this.auth.user();
    const checkout = new window.Razorpay({
      key,
      amount: Math.round(response.amount * 100),
      currency: response.currency,
      name: this.site.settings().businessName,
      description: `Order ${response.orderNumber}`,
      order_id: response.providerOrderId,
      image: '/media/seed/logo.svg',
      prefill: {
        name: user ? `${user.firstName} ${user.lastName}`.trim() : '',
        email: user?.email ?? '',
        contact: user?.phone ?? '',
      },
      notes: { orderNumber: response.orderNumber },
      theme: { color: '#8e4a5b' },
      handler: (result: RazorpaySuccess) => void this.verifyRazorpayPayment(response, result),
      modal: {
        ondismiss: () => {
          this.placing.set(false);
          this.toast.info(
            'Payment window closed. Your order is saved — use “Retry payment” whenever you are ready.',
            'Payment not completed',
          );
        },
      },
    });
    checkout.on('payment.failed', () => {
      this.placing.set(false);
      this.toast.error('The payment did not go through. Your order is saved — you can retry.', 'Payment failed');
    });
    checkout.open();
  }

  private async verifyRazorpayPayment(response: CheckoutResponse, result: RazorpaySuccess): Promise<void> {
    try {
      const order = await this.api.post<Order>('payments/verify', {
        orderId: response.orderId,
        providerOrderId: result.razorpay_order_id,
        providerPaymentId: result.razorpay_payment_id,
        signature: result.razorpay_signature,
        method: 'razorpay',
      });
      this.finish(order.id, order.orderNumber);
    } catch (error) {
      this.toast.error(
        `${toApiError(error).message} If money left your account, it will reconcile automatically — keep order ${response.orderNumber}.`,
        'We could not confirm the payment yet',
      );
      this.placing.set(false);
    }
  }

  protected retryPayment(): void {
    const pending = this.pendingOrder();
    if (!pending) return;
    this.errors.set([]);
    this.placing.set(true);
    if (pending.provider === 'Mock' || !pending.requiresPayment) {
      void this.completeMockPayment(pending);
    } else {
      void this.payWithRazorpay(pending);
    }
  }

  private finish(orderId: string, orderNumber: string): void {
    this.cart.clear();
    this.toast.success(`Payment received for ${orderNumber}. A confirmation is on its way.`, 'Order confirmed');
    this.analytics.track('checkout_complete', { metadata: { orderNumber } });
    void this.router.navigate(['/order', orderId], { queryParams: { new: 1 } });
  }

  private loadRazorpayScript(): Promise<boolean> {
    if (typeof window === 'undefined') return Promise.resolve(false);
    if (window.Razorpay) return Promise.resolve(true);
    return new Promise((resolve) => {
      const existing = document.querySelector<HTMLScriptElement>('script[data-razorpay-checkout]');
      if (existing) {
        existing.addEventListener('load', () => resolve(!!window.Razorpay));
        existing.addEventListener('error', () => resolve(false));
        return;
      }
      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.async = true;
      script.dataset['razorpayCheckout'] = 'true';
      script.addEventListener('load', () => resolve(!!window.Razorpay));
      script.addEventListener('error', () => resolve(false));
      document.body.appendChild(script);
    });
  }

  protected lineImage(url?: string | null): string {
    return url || '/media/seed/product-1.jpg';
  }

  protected errorFor(control: AbstractControl | null, label = 'This field'): string {
    if (!control || !control.touched || control.valid) return '';
    if (control.hasError('required')) return `${label} is required.`;
    if (control.hasError('maxlength')) return `${label} is a little too long.`;
    if (control.hasError('pattern')) return `${label} does not look right.`;
    return `Please check ${label.toLowerCase()}.`;
  }

  protected fieldError(group: FormGroup, name: string): string {
    const control = group.get(name);
    return this.errorFor(control, this.prettyField(name));
  }

  private prettyField(name: string): string {
    const map: Record<string, string> = {
      fullName: 'Full name',
      phone: 'Phone',
      addressLine1: 'Address line 1',
      addressLine2: 'Address line 2',
      city: 'City',
      state: 'State',
      postalCode: 'PIN code',
      country: 'Country',
    };
    return map[name] ?? 'This field';
  }

  private scrollTo(id: string): void {
    if (typeof document === 'undefined') return;
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
