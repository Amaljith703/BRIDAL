import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AnalyticsService } from '../../core/analytics.service';
import { SiteService } from '../../core/site.service';
import { ToastService } from '../../core/toast.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-contact-page',
  imports: [RouterLink, ReactiveFormsModule, PageHeroComponent, IconComponent, RevealDirective],
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss',
})
export class ContactPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly analytics = inject(AnalyticsService);
  protected readonly site = inject(SiteService);

  protected readonly sending = signal(false);
  protected readonly sent = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.maxLength(30)]],
    subject: ['', [Validators.required, Validators.maxLength(200)]],
    message: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(5000)]],
  });

  protected readonly subjects = [
    'Bridal enquiry',
    'Editorial or campaign',
    'Lesson or masterclass',
    'Product question',
    'Order or delivery',
    'Something else',
  ];

  protected readonly visitNotes = [
    { icon: 'map-pin', title: 'Finding us', copy: 'We are on the first floor above the flower shop. Look for the brass “MA” plaque and ring the studio bell.' },
    { icon: 'clock', title: 'Appointments', copy: 'Artistry is by appointment so the chair is always ready for you. Product pickups are welcome during studio hours.' },
    { icon: 'truck', title: 'Parking', copy: 'Two visitor bays sit behind the building. Metered street parking is available on the adjacent lane.' },
    { icon: 'message', title: 'Accessibility', copy: 'The studio is step-free via the side entrance. Message us ahead and we will have the ramp ready.' },
  ];

  protected readonly hours = [
    { day: 'Monday – Friday', time: '9:30 am – 7:00 pm' },
    { day: 'Saturday', time: '8:30 am – 8:00 pm' },
    { day: 'Sunday', time: 'By appointment only' },
  ];

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please complete the highlighted fields — a few details are still missing.', 'Almost there');
      return;
    }
    this.sending.set(true);
    try {
      const response = await this.api.post<{ message: string }>('contact', this.form.getRawValue());
      this.sent.set(true);
      this.toast.success(response.message || 'Your note is with the studio.', 'Message sent');
      this.analytics.track('contact_submit');
      this.form.reset();
    } catch (error) {
      this.toast.error(toApiError(error).message, 'Could not send your note');
    } finally {
      this.sending.set(false);
    }
  }

  protected sendAnother(): void {
    this.sent.set(false);
  }

  protected fieldError(name: 'name' | 'email' | 'phone' | 'subject' | 'message'): string {
    const control = this.form.controls[name];
    if (!control.touched || control.valid) return '';
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('email')) return 'Enter a valid email address.';
    if (control.hasError('minlength')) return 'A few more words, please.';
    if (control.hasError('maxlength')) return 'That is a little too long.';
    return 'Please check this field.';
  }
}
