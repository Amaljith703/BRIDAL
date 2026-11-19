import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { Booking } from '../../core/models';
import { DateLabelPipe } from '../../shared/pipes';

@Component({
  selector: 'app-account-bookings',
  imports: [RouterLink, DateLabelPipe],
  template: `
    <section class="account-page"><header class="page-head"><div><p class="eyebrow">A little time for you</p><h2>Your appointments</h2><p>Keep an eye on dates and requests with the studio.</p></div><a routerLink="/booking" class="btn btn--primary">Request a booking</a></header>
      @if(error()){<div class="notice--error" role="alert">{{error()}}</div>}
      @if(loading()){<p class="muted" role="status">Loading appointments…</p>}
      @else if(bookings().length){<div class="booking-list">@for(booking of bookings();track booking.id){<article class="booking-card"><div class="booking-date"><strong>{{booking.preferredDate | dateLabel}}</strong><span>{{booking.preferredTime}}</span></div><div class="booking-info"><p class="booking-name">{{booking.packageName || booking.serviceName || 'Studio appointment'}}</p><p>{{booking.bookingNumber}} · {{booking.occasion || 'Appointment'}}</p>@if(booking.notes){<p class="booking-note">{{booking.notes}}</p>}</div><span class="status-pill">{{booking.status}}</span></article>}</div>}
      @else{<div class="empty-state"><h3>No appointments yet</h3><p>Tell us about the look you have in mind and we will check the studio diary.</p><a routerLink="/booking" class="btn btn--primary">Book an appointment</a></div>}
      <p class="help-note">Need to adjust a date? <a routerLink="/support">Message the studio</a> and we will help.</p>
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:14px}.page-head{display:flex;justify-content:space-between;align-items:flex-end;gap:12px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.booking-list{display:grid;gap:10px}.booking-card{display:grid;grid-template-columns:minmax(110px,.55fr) minmax(0,1fr) auto;align-items:center;gap:15px;padding:17px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.booking-date{display:grid;gap:5px;color:#8e4a5b}.booking-date strong{font:500 1rem Georgia,serif}.booking-date span,.booking-info p:not(.booking-name){color:#806e72;font-size:.76rem}.booking-info p{margin:3px 0}.booking-name{font-weight:700}.booking-note{color:#705d61;font-size:.78rem}.status-pill{padding:6px 10px;border-radius:999px;background:#f5eeeb;font-size:.72rem}.empty-state{padding:38px 18px;border:1px dashed #d9c8c2;border-radius:14px;text-align:center}.empty-state p,.help-note{color:#806e72;font-size:.84rem}.help-note a{color:#8e4a5b;font-weight:700}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}@media(max-width:600px){.page-head{align-items:flex-start;flex-direction:column}.booking-card{grid-template-columns:1fr auto;gap:8px}.booking-info{grid-column:1/-1;grid-row:2}.status-pill{grid-column:2;grid-row:1}}
  `,
})
export class AccountBookingsComponent implements OnInit {
  private readonly api = inject(ApiService);
  protected readonly bookings = signal<Booking[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }
  private async load(): Promise<void> {
    try { this.bookings.set(await this.api.get<Booking[]>('account/bookings')); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.loading.set(false); }
  }
}
