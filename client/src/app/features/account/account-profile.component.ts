import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { toApiError } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-account-profile',
  imports: [ReactiveFormsModule],
  template: `
    <section class="account-page"><header class="page-head"><div><p class="eyebrow">The details we keep</p><h2>Your profile</h2><p>Keep the best way to reach you up to date.</p></div></header>
      @if(error()){<div class="notice--error" role="alert">{{error()}}</div>}
      <form class="panel profile-form" [formGroup]="form" (ngSubmit)="save()">
        <label>Email address<input class="input" [value]="auth.user()?.email || ''" readonly aria-describedby="email-help" /></label><small id="email-help" class="muted">Your sign-in email cannot be changed here.</small>
        <div class="field-grid"><label>First name<input class="input" formControlName="firstName" autocomplete="given-name" maxlength="80" required /></label><label>Last name<input class="input" formControlName="lastName" autocomplete="family-name" maxlength="80" required /></label></div>
        <label>Phone<input class="input" formControlName="phone" autocomplete="tel" maxlength="30" /></label>
        <label class="check"><input type="checkbox" formControlName="marketingOptIn" /> Send me studio notes and occasional offers.</label>
        <button type="submit" class="btn btn--primary" [disabled]="saving()">{{saving()?'Saving…':'Save profile'}}</button>
      </form>
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:14px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.panel{display:grid;gap:14px;padding:20px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.panel label{display:grid;gap:6px;font-size:.8rem;font-weight:600}.input{width:100%;padding:11px 12px;border:1px solid #ded0ca;border-radius:9px;background:#fff;font:inherit}.input:focus{outline:3px solid rgba(142,74,91,.16);border-color:#8e4a5b}.input[readonly]{background:#f5f1ef;color:#806e72}.field-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.muted{color:#806e72;font-size:.75rem}.check{display:flex!important;align-items:center;gap:9px;color:#705d61;font-weight:400!important}.check input{accent-color:#8e4a5b}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}@media(max-width:520px){.field-grid{grid-template-columns:1fr}}
  `,
})
export class AccountProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  protected readonly auth = inject(AuthService);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(80)]],
    lastName: ['', [Validators.required, Validators.maxLength(80)]],
    phone: ['', [Validators.maxLength(30)]],
    marketingOptIn: [false],
  });

  ngOnInit(): void {
    const user = this.auth.user();
    if (user) this.form.reset({ firstName: user.firstName, lastName: user.lastName, phone: user.phone ?? '', marketingOptIn: user.marketingOptIn });
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); this.error.set(null);
    try { await this.auth.updateProfile(this.form.getRawValue()); this.toast.success('Your profile details have been updated.', 'Profile saved'); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.saving.set(false); }
  }
}
