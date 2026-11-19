import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, toApiError } from '../../../core/api.service';
import { Booking, BookingStatus, Paged } from '../../../core/models';
import { DateLabelPipe } from '../../../shared/pipes';
import { ToastService } from '../../../core/toast.service';

@Component({
  selector: 'app-admin-bookings',
  imports: [FormsModule, DateLabelPipe],
  template: `<section class="admin-page"><header class="admin-page-head"><div><p class="eyebrow">Studio calendar</p><h1>Appointments</h1><p>Review client requests, confirm a time, and keep appointment notes together.</p></div><button class="admin-button" (click)="load()">Refresh</button></header>
    <div class="admin-toolbar"><label class="admin-search">Search appointments<input [(ngModel)]="search" (keyup.enter)="page.set(1); load()" placeholder="Name, phone or reference" /></label><label>Status<select [(ngModel)]="status" (change)="page.set(1); load()"><option value="">All statuses</option>@for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }</select></label><button class="admin-button primary" (click)="page.set(1); load()">Apply</button></div>
    @if (error()) { <div class="admin-alert" role="alert">{{ error() }}</div> }<div class="admin-panel"><div class="admin-panel-head"><div><h2>Booking requests</h2><p>{{ result()?.totalCount ?? 0 }} requests</p></div></div>
      @if (loading()) { <p class="admin-empty">Loading appointments…</p> } @else if (result()?.items?.length) { <div class="admin-table-wrap"><table class="admin-table"><thead><tr><th>Client</th><th>Appointment</th><th>Requested service</th><th>Request</th><th>Status</th><th>Manage</th></tr></thead><tbody>@for (b of result()!.items; track b.id) { <tr><td><strong>{{ b.fullName }}</strong><small><a [href]="'mailto:' + b.email">{{ b.email }}</a> · {{ b.phone }}</small></td><td>{{ b.preferredDate | dateLabel }}<small>{{ b.preferredTime }}</small></td><td>{{ b.packageName || b.serviceName || 'Studio appointment' }}<small>{{ b.occasion || 'Occasion not specified' }}</small></td><td>{{ b.notes || '—' }}</td><td><span class="admin-pill" [class.attention]="b.status === 'Pending'">{{ b.status }}</span></td><td><div class="admin-inline"><select [ngModel]="b.status" #statusSelect><option value="Pending">Pending</option><option value="Confirmed">Confirmed</option><option value="Completed">Completed</option><option value="Cancelled">Cancelled</option></select><button class="admin-button small" (click)="update(b, statusSelect.value)">Save</button></div><textarea [ngModel]="b.adminNotes || ''" #notes rows="2" placeholder="Private studio note"></textarea><button class="admin-link-button" (click)="update(b, statusSelect.value, notes.value)">Save note</button></td></tr> }</tbody></table></div> } @else { <p class="admin-empty">No appointments match these filters.</p> }
      <footer class="admin-pager"><span>Page {{ result()?.page ?? 1 }} of {{ result()?.totalPages ?? 1 }}</span><div><button class="admin-button" [disabled]="page() <= 1" (click)="page.update(v => v - 1); load()">Previous</button><button class="admin-button" [disabled]="!result()?.hasNext" (click)="page.update(v => v + 1); load()">Next</button></div></footer></div></section>`,
})
export class AdminBookingsComponent {
  private readonly api = inject(ApiService); private readonly toast = inject(ToastService);
  protected readonly statuses: BookingStatus[] = ['Pending', 'Confirmed', 'Completed', 'Cancelled'];
  protected readonly result = signal<Paged<Booking> | null>(null); protected readonly loading = signal(true); protected readonly error = signal<string | null>(null); protected readonly page = signal(1);
  protected search = ''; protected status = '';
  constructor() { void this.load(); }
  protected async load(): Promise<void> { this.loading.set(true); this.error.set(null); try { this.result.set(await this.api.get<Paged<Booking>>('admin/bookings', { page: this.page(), pageSize: 20, search: this.search, status: this.status })); } catch (e) { this.error.set(toApiError(e).message); } finally { this.loading.set(false); } }
  protected async update(booking: Booking, status: string, adminNotes?: string): Promise<void> { try { await this.api.put(`admin/bookings/${booking.id}`, { status, adminNotes }); this.toast.success('Appointment information saved.'); await this.load(); } catch (e) { this.toast.error(toApiError(e).message); } }
}
