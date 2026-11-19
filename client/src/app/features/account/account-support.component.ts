import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, toApiError } from '../../core/api.service';
import { Paged, SupportTicket, SupportTicketSummary } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { DateLabelPipe } from '../../shared/pipes';

@Component({
  selector: 'app-account-support',
  imports: [ReactiveFormsModule, DateLabelPipe],
  template: `
    <section class="account-page"><header class="page-head"><div><p class="eyebrow">Here when you need us</p><h2>Support tickets</h2><p>Ask about an order, delivery, payment or appointment.</p></div><button type="button" class="btn btn--primary" (click)="newTicket.set(!newTicket())">{{newTicket()?'Close':'New ticket'}}</button></header>
      @if(error()){<div class="notice--error" role="alert">{{error()}}</div>}
      @if(newTicket()){<section class="panel"><h3>Tell us what happened</h3><form [formGroup]="createForm" (ngSubmit)="create()"><label>Subject<input class="input" formControlName="subject" maxlength="200" required /></label><label>Category<select class="input" formControlName="category"><option>Order</option><option>Delivery</option><option>Payment</option><option>Booking</option><option>Product</option><option>Account</option><option>Other</option></select></label><label>Message<textarea class="input" formControlName="message" rows="4" minlength="10" maxlength="5000" required></textarea></label><button type="submit" class="btn btn--primary" [disabled]="busy()">{{busy()?'Sending…':'Send to the studio'}}</button></form></section>}
      <div class="ticket-layout">
        <section class="ticket-list" aria-label="Your support tickets">
          @if(loading()){<p class="muted" role="status">Loading tickets…</p>}
          @else if(tickets().items.length){@for(ticket of tickets().items;track ticket.id){<button type="button" class="ticket-button" [class.is-selected]="selectedId()===ticket.id" (click)="open(ticket.id)"><span><strong>{{ticket.subject}}</strong><small>{{ticket.ticketNumber}} · {{ticket.category}}</small></span><span class="status-pill">{{ticket.status}}</span></button>}}
          @else{<p class="empty-note">You have not opened a support ticket yet.</p>}
        </section>
        <section class="ticket-detail">
          @if(detail();as ticket){<header class="detail-head"><div><h3>{{ticket.subject}}</h3><p>{{ticket.ticketNumber}} · {{ticket.status}} · {{ticket.priority}}</p></div></header><div class="messages">@for(message of ticket.messages;track message.id){<article class="message"><div><strong>{{message.senderName}}</strong><small>{{message.createdAtUtc | dateLabel:true}}</small></div><p>{{message.body}}</p></article>}</div>
            @if(ticket.status!=='Closed'){<form class="reply-form" [formGroup]="replyForm" (ngSubmit)="reply(ticket.id)"><label for="ticket-reply">Add a reply</label><textarea id="ticket-reply" class="input" formControlName="message" rows="3" maxlength="5000" required></textarea><button type="submit" class="btn btn--ghost" [disabled]="busy()">Send reply</button></form>}
          } @else {<div class="empty-detail"><h3>Choose a ticket</h3><p>Open a conversation to read or reply to it.</p></div>}
        </section>
      </div>
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:14px}.page-head{display:flex;align-items:flex-end;justify-content:space-between;gap:12px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.panel,.ticket-list,.ticket-detail{padding:18px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.panel h3,.detail-head h3,.empty-detail h3{margin:0;font:500 1.15rem Georgia,serif}.panel form,.reply-form{display:grid;gap:11px;margin-top:13px}.panel label,.reply-form label{display:grid;gap:6px;font-size:.8rem;font-weight:600}.input{width:100%;padding:10px 12px;border:1px solid #ded0ca;border-radius:9px;background:#fff;font:inherit}.ticket-layout{display:grid;grid-template-columns:minmax(210px,.75fr) minmax(0,1.6fr);gap:12px;align-items:start}.ticket-list{display:grid;gap:6px}.ticket-button{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:11px;border:1px solid transparent;border-radius:10px;background:transparent;color:#3d2a2f;text-align:left;cursor:pointer}.ticket-button:hover,.ticket-button.is-selected{border-color:#eadfd9;background:#f8f1eb}.ticket-button>span:first-child{display:grid;gap:5px;min-width:0}.ticket-button strong{overflow:hidden;font-size:.79rem;text-overflow:ellipsis;white-space:nowrap}.ticket-button small{color:#806e72;font-size:.68rem}.status-pill{padding:5px 8px;border-radius:999px;background:#f5eeeb;font-size:.67rem}.detail-head{padding-bottom:12px;border-bottom:1px solid #f0e8e3}.detail-head p{margin:5px 0 0;color:#806e72;font-size:.74rem}.messages{display:grid;gap:10px;padding-block:14px}.message{padding:12px;border-radius:10px;background:#f8f1eb}.message>div{display:flex;justify-content:space-between;gap:8px}.message strong{font-size:.78rem}.message small{color:#806e72;font-size:.68rem}.message p{margin:8px 0 0;color:#705d61;font-size:.83rem;line-height:1.55;white-space:pre-wrap}.reply-form{padding-top:12px;border-top:1px solid #f0e8e3}.empty-note,.empty-detail p,.muted{color:#806e72;font-size:.83rem}.empty-detail{display:grid;min-height:220px;place-content:center;text-align:center}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}@media(max-width:640px){.page-head{align-items:flex-start;flex-direction:column}.ticket-layout{grid-template-columns:1fr}.ticket-list{max-height:260px;overflow:auto}}
  `,
})
export class AccountSupportComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly newTicket = signal(false);
  protected readonly tickets = signal<Paged<SupportTicketSummary>>({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0, hasNext: false, hasPrevious: false });
  protected readonly selectedId = signal<string | null>(null);
  protected readonly detail = signal<SupportTicket | null>(null);
  protected readonly createForm = this.fb.nonNullable.group({ subject: ['', [Validators.required, Validators.maxLength(200)]], category: ['Order', Validators.required], message: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(5000)]] });
  protected readonly replyForm = this.fb.nonNullable.group({ message: ['', [Validators.required, Validators.maxLength(5000)]] });

  ngOnInit(): void { void this.load(); }
  protected async open(id: string): Promise<void> { this.selectedId.set(id); this.error.set(null); try { this.detail.set(await this.api.get<SupportTicket>(`account/support/tickets/${id}`)); } catch (error) { this.error.set(toApiError(error).message); } }
  protected async create(): Promise<void> {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.busy.set(true); this.error.set(null);
    try { const ticket = await this.api.post<SupportTicket>('account/support/tickets', this.createForm.getRawValue()); this.createForm.reset({ subject: '', category: 'Order', message: '' }); this.newTicket.set(false); await this.load(); await this.open(ticket.id); this.toast.success('Your note is with the studio. We will reply here.', 'Ticket opened'); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
  protected async reply(id: string): Promise<void> {
    if (this.replyForm.invalid) { this.replyForm.markAllAsTouched(); return; }
    this.busy.set(true); this.error.set(null);
    try { this.detail.set(await this.api.post<SupportTicket>(`account/support/tickets/${id}/messages`, this.replyForm.getRawValue())); this.replyForm.reset({ message: '' }); await this.load(); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
  private async load(): Promise<void> {
    this.loading.set(true); this.error.set(null);
    try { this.tickets.set(await this.api.get<Paged<SupportTicketSummary>>('account/support/tickets', { page: 1, pageSize: 50 })); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.loading.set(false); }
  }
}
