import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, toApiError } from '../../../core/api.service';
import { AuditLog, Paged } from '../../../core/models';
import { DateLabelPipe } from '../../../shared/pipes';

@Component({
  selector: 'app-admin-audit',
  imports: [FormsModule, DateLabelPipe],
  template: `<section class="admin-page"><header class="admin-page-head"><div><p class="eyebrow">System activity</p><h1>Audit log</h1><p>Review recent administrative changes and the account that made them.</p></div><button class="admin-button" (click)="load()">Refresh</button></header><div class="admin-toolbar"><label class="admin-search">Search activity<input [(ngModel)]="search" (keyup.enter)="page.set(1); load()" placeholder="Action, record or account" /></label><button class="admin-button primary" (click)="page.set(1); load()">Search</button></div>@if (error()) { <div class="admin-alert" role="alert">{{ error() }}</div> }<div class="admin-panel"><div class="admin-panel-head"><div><h2>Activity</h2><p>{{ result()?.totalCount ?? 0 }} records</p></div></div>@if (loading()) { <p class="admin-empty">Loading audit records…</p> } @else if (result()?.items?.length) { <div class="admin-table-wrap"><table class="admin-table"><thead><tr><th>Action</th><th>Record</th><th>Details</th><th>Account</th><th>IP address</th><th>Time</th></tr></thead><tbody>@for (item of result()!.items; track item.id) { <tr><td><strong>{{ item.action }}</strong></td><td>{{ item.entityName }}<small>{{ item.entityId || '—' }}</small></td><td class="message-cell">{{ item.details || '—' }}</td><td>{{ item.userEmail || 'System' }}</td><td>{{ item.ipAddress || '—' }}</td><td>{{ item.createdAtUtc | dateLabel:true }}</td></tr> }</tbody></table></div> } @else { <p class="admin-empty">No activity found.</p> }<footer class="admin-pager"><span>Page {{ result()?.page ?? 1 }} of {{ result()?.totalPages ?? 1 }}</span><div><button class="admin-button" [disabled]="page() <= 1" (click)="page.update(v => v - 1); load()">Previous</button><button class="admin-button" [disabled]="!result()?.hasNext" (click)="page.update(v => v + 1); load()">Next</button></div></footer></div></section>`,
})
export class AdminAuditComponent {
  private readonly api = inject(ApiService); protected readonly result = signal<Paged<AuditLog> | null>(null); protected readonly loading = signal(true); protected readonly error = signal<string | null>(null); protected readonly page = signal(1); protected search = '';
  constructor() { void this.load(); }
  protected async load(): Promise<void> { this.loading.set(true); this.error.set(null); try { this.result.set(await this.api.get<Paged<AuditLog>>('admin/audit-logs', { page: this.page(), pageSize: 30, search: this.search })); } catch (e) { this.error.set(toApiError(e).message); } finally { this.loading.set(false); } }
}
