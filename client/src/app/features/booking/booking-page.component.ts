import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { AuthService } from '../../core/auth.service';
import { Booking, Paged, Service, ServicePackage } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { DurationPipe, InrPipe, DateLabelPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

type BookingKind = 'package' | 'service';

@Component({
  selector: 'app-booking-page',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    PageHeroComponent,
    IconComponent,
    ImgFallbackDirective,
    DurationPipe,
    InrPipe,
    DateLabelPipe,
    RevealDirective,
  ],
  templateUrl: './booking-page.component.html',
  styleUrl: './booking-page.component.scss',
})
export class BookingPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly auth = inject(AuthService);
  protected readonly site = inject(SiteService);

  protected readonly step = signal(1);
  protected readonly kind = signal<BookingKind>('package');
  protected readonly packages = signal<ServicePackage[]>([]);
  protected readonly services = signal<Service[]>([]);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly booking = signal<Booking | null>(null);
  protected readonly selectedPackageId = signal<string | null>(null);
  protected readonly selectedServiceId = signal<string | null>(null);
  protected readonly slot = signal('');

  protected readonly occasions = ['Bridal', 'Reception', 'Engagement', 'Editorial', 'Party', 'Lesson', 'Other'];
  protected readonly slots = ['07:00', '08:00', '09:00', '10:00', '11:00', '12:00', '14:00', '15:00', '16:00', '17:00', '18:00'];

  protected readonly selectedPackage = computed(
    () => this.packages().find((item) => item.id === this.selectedPackageId()) ?? null,
  );
  protected readonly selectedService = computed(
    () => this.services().find((item) => item.id === this.selectedServiceId()) ?? null,
  );
  protected readonly selectionName = computed(
    () => this.selectedPackage()?.name ?? this.selectedService()?.name ?? '',
  );
  protected readonly selectionPrice = computed(
    () => this.selectedPackage()?.price ?? this.selectedService()?.price ?? 0,
  );
  protected readonly selectionDuration = computed(
    () => this.selectedPackage()?.durationMinutes ?? this.selectedService()?.durationMinutes ?? 0,
  );

  protected readonly minDate = this.toInputDate(new Date());
  protected readonly maxDate = this.toInputDate(new Date(new Date().setMonth(new Date().getMonth() + 18)));

  protected readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.maxLength(30)]],
    preferredDate: ['', [Validators.required]],
    preferredTime: ['10:00', [Validators.required]],
    occasion: ['Bridal'],
    notes: ['', [Validators.maxLength(1500)]],
  });

  protected readonly nextSteps = [
    { title: 'We review availability', copy: 'An artist checks the studio diary and the time needed for your look.' },
    { title: 'You receive a confirmation', copy: 'We email or WhatsApp within one working day with the confirmed time and artist.' },
    { title: 'A 30% advance holds the date', copy: 'For bridal and package bookings, the deposit secures your date. The balance is due on the day.' },
    { title: 'Trial, if you want one', copy: 'We can add a trial session at a preferred rate so the morning itself is entirely calm.' },
  ];

  protected readonly goodToKnow = [
    'Bookings are open up to 18 months ahead.',
    'Complimentary travel within Bengaluru city limits.',
    'Reschedule once with 7 days’ notice at no charge.',
    'Your details are only used for this appointment.',
  ];

  constructor() {
    const query = this.route.snapshot.queryParamMap;
    const packageId = query.get('package');
    const serviceId = query.get('service');
    void this.load(packageId, serviceId).then(() => this.prefill(packageId, serviceId));
    this.analytics.track('booking_open');
  }

  private async load(packageId: string | null, serviceId: string | null): Promise<void> {
    this.loading.set(true);
    try {
      const [packages, services] = await Promise.allSettled([
        this.api.get<Paged<ServicePackage>>('catalog/packages', { page: 1, pageSize: 24 }),
        this.api.get<Paged<Service>>('catalog/services', { page: 1, pageSize: 48 }),
      ]);
      if (packages.status === 'fulfilled') this.packages.set(packages.value.items);
      if (services.status === 'fulfilled') this.services.set(services.value.items);
      if (packageId && this.packages().some((item) => item.id === packageId)) {
        this.kind.set('package');
        this.selectedPackageId.set(packageId);
      } else if (serviceId && this.services().some((item) => item.id === serviceId)) {
        this.kind.set('service');
        this.selectedServiceId.set(serviceId);
      }
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not load the menu');
    } finally {
      this.loading.set(false);
    }
  }

  private prefill(packageId: string | null, serviceId: string | null): void {
    const user = this.auth.user();
    if (user) {
      this.form.patchValue({
        fullName: `${user.firstName} ${user.lastName}`.trim(),
        email: user.email,
        phone: user.phone ?? '',
      });
    }
    if (packageId || serviceId) {
      this.step.set(2);
    }
  }

  protected setKind(kind: BookingKind): void {
    this.kind.set(kind);
  }

  protected choosePackage(item: ServicePackage): void {
    this.kind.set('package');
    this.selectedPackageId.set(item.id);
    this.selectedServiceId.set(null);
  }

  protected chooseService(item: Service): void {
    this.kind.set('service');
    this.selectedServiceId.set(item.id);
    this.selectedPackageId.set(null);
  }

  protected chooseSlot(value: string): void {
    this.slot.set(value);
    this.form.controls.preferredTime.setValue(value);
  }

  protected next(): void {
    if (this.step() === 1) {
      if (!this.selectionName()) {
        this.toast.warning('Choose a package or a single service first.', 'One more thing');
        return;
      }
      this.step.set(2);
      this.scrollTop();
      return;
    }
    if (this.step() === 2) {
      this.form.controls.preferredDate.markAsTouched();
      this.form.controls.preferredTime.markAsTouched();
      if (!this.form.controls.preferredDate.value) {
        this.toast.warning('Pick the date you have in mind — it helps us check the diary.', 'Choose a date');
        return;
      }
      this.step.set(3);
      this.scrollTop();
    }
  }

  protected back(): void {
    this.step.update((value) => Math.max(1, value - 1));
    this.scrollTop();
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please complete the highlighted fields.', 'Almost there');
      return;
    }
    if (!this.selectionName()) {
      this.step.set(1);
      this.toast.warning('Choose what you would like to book.', 'One more thing');
      return;
    }
    this.submitting.set(true);
    const value = this.form.getRawValue();
    const payload = {
      fullName: value.fullName.trim(),
      email: value.email.trim(),
      phone: value.phone.trim(),
      serviceId: this.selectedServiceId(),
      packageId: this.selectedPackageId(),
      preferredDate: new Date(`${value.preferredDate}T00:00:00`).toISOString(),
      preferredTime: value.preferredTime,
      occasion: value.occasion || null,
      notes: value.notes?.trim() || null,
    };
    try {
      const booking = await this.api.post<Booking>('bookings', payload);
      this.booking.set(booking);
      this.analytics.track('booking_submit', {
        metadata: { bookingNumber: booking.bookingNumber, item: this.selectionName() },
      });
      this.scrollTop();
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not request the date');
    } finally {
      this.submitting.set(false);
    }
  }

  protected fieldError(name: 'fullName' | 'email' | 'phone' | 'preferredDate' | 'notes'): string {
    const control = this.form.controls[name];
    if (!control.touched || control.valid) return '';
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('email')) return 'Enter a valid email address.';
    if (control.hasError('maxlength')) return 'That is a little too long.';
    return 'Please check this field.';
  }

  protected prettyDate(value: string): string {
    if (!value) return 'To be chosen';
    const date = new Date(`${value}T00:00:00`);
    return date.toLocaleDateString('en-IN', { weekday: 'short', day: 'numeric', month: 'long', year: 'numeric' });
  }

  protected prettyTime(value: string): string {
    if (!value) return 'To be chosen';
    const [hour, minute] = value.split(':').map(Number);
    const date = new Date();
    date.setHours(hour, minute, 0, 0);
    return date.toLocaleTimeString('en-IN', { hour: 'numeric', minute: '2-digit', hour12: true });
  }

  private scrollTop(): void {
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 320, behavior: 'smooth' });
    }
  }

  private toInputDate(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
