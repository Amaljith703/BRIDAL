import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { AuthService } from '../../core/auth.service';
import { Faq, Paged, SupportTicket } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-support-page',
  imports: [RouterLink, ReactiveFormsModule, PageHeroComponent, IconComponent, RevealDirective],
  templateUrl: './support-page.component.html',
  styleUrl: './support-page.component.scss',
})
export class SupportPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly auth = inject(AuthService);
  protected readonly site = inject(SiteService);

  protected readonly submitting = signal(false);
  protected readonly ticket = signal<SupportTicket | null>(null);
  protected readonly faqs = signal<Faq[]>([]);
  protected readonly openFaq = signal<string | null>(null);

  protected readonly categories = ['Order', 'Delivery', 'Payment', 'Booking', 'Product', 'Account', 'Other'];

  protected readonly channels = [
    {
      icon: 'whatsapp',
      title: 'WhatsApp',
      copy: 'Fastest for order updates and quick questions.',
      action: 'Message the studio',
      href: () => this.site.whatsAppUrl(),
      external: true,
    },
    {
      icon: 'mail',
      title: 'Email',
      copy: 'Best for detailed notes, invoices and returns.',
      action: 'Write to the studio',
      href: () => `mailto:${this.site.settings().supportEmail || this.site.settings().email}`,
      external: false,
    },
    {
      icon: 'phone',
      title: 'Call',
      copy: 'Monday to Saturday, 9:30 am – 7 pm.',
      action: 'Call the studio',
      href: () => `tel:${this.site.settings().supportPhone || this.site.settings().phone}`,
      external: false,
    },
  ];

  protected readonly responseTimes = [
    { channel: 'WhatsApp', time: 'Within a few hours' },
    { channel: 'Email', time: 'Within one working day' },
    { channel: 'Support ticket', time: 'Within one working day' },
    { channel: 'Returns pickup', time: '2–4 working days' },
  ];

  protected readonly policies = [
    { icon: 'truck', title: 'Track an order', copy: 'Open My Orders for live status and the tracking number once your parcel leaves the studio.', link: '/account/orders', linkLabel: 'My orders' },
    { icon: 'refresh', title: 'Return or exchange', copy: 'Unopened products can be returned within 7 days with the invoice. Raise a ticket with your order number.', link: '/account/support', linkLabel: 'Raise a ticket' },
    { icon: 'calendar', title: 'Change a booking', copy: 'Reschedule once with 7 days’ notice at no charge. Message the studio and we will hold the new date.', link: '/booking', linkLabel: 'Booking form' },
    { icon: 'shield', title: 'Payment help', copy: 'Failed payments are released within 3–5 business days. Captured but not confirmed? Send us the reference.', link: '/faq', linkLabel: 'Payment FAQ' },
  ];

  protected readonly form = this.fb.nonNullable.group({
    subject: ['', [Validators.required, Validators.maxLength(200)]],
    category: ['Order', [Validators.required]],
    message: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(5000)]],
  });

  ngOnInit(): void {
    void this.loadFaqs();
    this.analytics.track('support_view');
  }

  private async loadFaqs(): Promise<void> {
    try {
      const result = await this.api.get<Paged<Faq>>('content/faqs', { page: 1, pageSize: 6 });
      this.faqs.set(result.items);
    } catch {
      this.faqs.set(this.site.faqs().slice(0, 6));
    }
  }

  protected toggleFaq(id: string): void {
    this.openFaq.update((current) => (current === id ? null : id));
  }

  protected async submit(): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.toast.info('Sign in first so your ticket is linked to your orders.', 'One quick step');
      void this.router.navigate(['/login'], { queryParams: { returnUrl: '/support' } });
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Add a subject and a few details so we can help quickly.', 'Almost there');
      return;
    }
    this.submitting.set(true);
    try {
      const ticket = await this.api.post<SupportTicket>('account/support/tickets', this.form.getRawValue());
      this.ticket.set(ticket);
      this.form.reset({ category: 'Order', subject: '', message: '' });
      this.analytics.track('support_ticket_create', { metadata: { ticketNumber: ticket.ticketNumber } });
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not raise the ticket');
    } finally {
      this.submitting.set(false);
    }
  }

  protected fieldError(name: 'subject' | 'message'): string {
    const control = this.form.controls[name];
    if (!control.touched || control.valid) return '';
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('minlength')) return 'A few more details, please.';
    if (control.hasError('maxlength')) return 'That is a little too long.';
    return 'Please check this field.';
  }
}
