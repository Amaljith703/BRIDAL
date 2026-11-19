import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import QRCode from 'qrcode';
import { AuthService } from '../../core/auth.service';
import { toApiError } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-account-security',
  imports: [ReactiveFormsModule],
  template: `
    <section class="account-page"><header class="page-head"><div><p class="eyebrow">Your account, protected</p><h2>Sign-in security</h2><p>Manage your password and authenticator app.</p></div></header>
      @if(error()){<div class="notice--error" role="alert">{{error()}}</div>}
      @if(message()){<div class="notice--success" role="status">{{message()}}</div>}
      <section class="panel"><div class="panel-head"><div><h3>Two-factor authentication</h3><p>Ask for a code from your authenticator app when signing in.</p></div><span class="state-pill" [class.is-on]="auth.user()?.twoFactorEnabled">{{auth.user()?.twoFactorEnabled?'On':'Off'}}</span></div>
        @if(!auth.user()?.twoFactorEnabled){
          @if(!setup()){<button type="button" class="btn btn--ghost" (click)="beginSetup()" [disabled]="busy()">Set up an authenticator app</button>}
          @else{<div class="setup-grid"><div class="qr-box">@if(qr()) { <img [src]="qr()" alt="QR code for authenticator app setup" width="190" height="190" /> }</div><div><p>Scan this QR code with your authenticator app. If you cannot scan it, enter this key manually:</p><code class="secret-key">{{setup()?.sharedKey}}</code><p class="muted">The key is sensitive. Do not share it with anyone.</p></div></div>
            <form [formGroup]="codeForm" (ngSubmit)="enableTwoFactor()"><label>Six-digit code<input class="input" formControlName="code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" required /></label><label>Password <small>For account verification</small><input class="input" type="password" formControlName="password" autocomplete="current-password" required /></label><button type="submit" class="btn btn--primary" [disabled]="busy()">{{busy()?'Verifying…':'Turn on two-factor authentication'}}</button></form>
          }
        } @else {
          <form [formGroup]="codeForm" (ngSubmit)="disableTwoFactor()"><p class="muted">Your authenticator app is connected. Save a new set of recovery codes if you have used or misplaced the old ones.</p><label>Password<input class="input" type="password" formControlName="password" autocomplete="current-password" required /></label><button type="submit" class="btn btn--ghost" [disabled]="busy()">Turn off two-factor authentication</button></form>
          <form [formGroup]="passwordOnlyForm" (ngSubmit)="regenerateRecoveryCodes()"><label>Current password<input class="input" type="password" formControlName="password" autocomplete="current-password" required /></label><button type="submit" class="text-button" [disabled]="busy()">Generate new recovery codes</button></form>
        }
        @if(recoveryCodes().length){<div class="recovery-box"><h4>Save these recovery codes</h4><p>Each code can be used once. This list will be shown only now.</p><ul>@for(code of recoveryCodes();track code){<li><code>{{code}}</code></li>}</ul><button type="button" class="text-button" (click)="recoveryCodes.set([])">I have saved them</button></div>}
      </section>

      <section class="panel"><h3>Change password</h3><p class="muted">After a password change, you will be signed out on all devices.</p><form [formGroup]="passwordForm" (ngSubmit)="changePassword()"><label>Current password<input class="input" type="password" formControlName="currentPassword" autocomplete="current-password" required /></label><div class="field-grid"><label>New password<input class="input" type="password" formControlName="newPassword" autocomplete="new-password" minlength="10" required /></label><label>Confirm new password<input class="input" type="password" formControlName="confirmPassword" autocomplete="new-password" required /></label></div><button type="submit" class="btn btn--primary" [disabled]="busy()">Change password</button></form></section>
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:14px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.panel{display:grid;gap:15px;padding:20px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.panel h3{margin:0;font:500 1.2rem Georgia,serif}.panel>p,.panel-head p{margin:5px 0 0;color:#806e72;font-size:.82rem}.panel-head{display:flex;justify-content:space-between;gap:12px}.state-pill{align-self:flex-start;padding:6px 10px;border-radius:999px;background:#f5eeeb;color:#806e72;font-size:.72rem}.state-pill.is-on{background:#e8f2ea;color:#315b43}.panel form{display:grid;gap:12px;max-width:540px}.panel label{display:grid;gap:6px;font-size:.8rem;font-weight:600}.panel label small,.muted{color:#806e72;font-size:.75rem;font-weight:400}.input{width:100%;padding:11px 12px;border:1px solid #ded0ca;border-radius:9px;background:#fff;font:inherit}.input:focus{outline:3px solid rgba(142,74,91,.16);border-color:#8e4a5b}.setup-grid{display:grid;grid-template-columns:210px 1fr;gap:20px;align-items:center}.qr-box{display:grid;width:210px;height:210px;place-items:center;border:1px solid #eadfd9;border-radius:14px;background:#fff}.secret-key{display:inline-block;padding:8px 10px;border-radius:7px;background:#f5eeeb;font-size:.9rem;letter-spacing:.12em}.recovery-box{padding:14px;border-radius:12px;background:#f5eeeb}.recovery-box h4{margin:0}.recovery-box p{color:#705d61;font-size:.8rem}.recovery-box ul{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:7px;margin:10px 0;padding:0;list-style:none}.recovery-box code{font-size:.8rem}.field-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.text-button{justify-self:start;padding:0;border:0;background:none;color:#8e4a5b;font:inherit;font-size:.8rem;font-weight:700;cursor:pointer}.notice--error,.notice--success{padding:12px;border-radius:10px}.notice--error{color:#7a2636;background:#fbe9ea}.notice--success{color:#315b43;background:#e8f2ea}@media(max-width:620px){.setup-grid{grid-template-columns:1fr}.qr-box{width:190px;height:190px}.field-grid{grid-template-columns:1fr}.recovery-box ul{grid-template-columns:1fr}}
  `,
})
export class AccountSecurityComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  protected readonly auth = inject(AuthService);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly setup = signal<{ sharedKey: string; authenticatorUri: string } | null>(null);
  protected readonly qr = signal<string | null>(null);
  protected readonly recoveryCodes = signal<string[]>([]);
  protected readonly codeForm = this.fb.nonNullable.group({ code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]], password: ['', Validators.required] });
  protected readonly passwordOnlyForm = this.fb.nonNullable.group({ password: ['', Validators.required] });
  protected readonly passwordForm = this.fb.nonNullable.group({ currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(10)]], confirmPassword: ['', Validators.required] });

  protected async beginSetup(): Promise<void> {
    this.busy.set(true); this.error.set(null);
    try { const setup = await this.auth.twoFactorSetup(); this.setup.set(setup); this.qr.set(await QRCode.toDataURL(setup.authenticatorUri, { width: 190, margin: 1 })); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
  protected async enableTwoFactor(): Promise<void> {
    if (this.codeForm.invalid) { this.codeForm.markAllAsTouched(); return; }
    this.busy.set(true); this.error.set(null);
    try { const result = await this.auth.enableTwoFactor(this.codeForm.controls.code.value.trim(), this.codeForm.controls.password.value); this.recoveryCodes.set(result.recoveryCodes); await this.auth.refresh(); this.setup.set(null); this.qr.set(null); this.codeForm.reset(); this.message.set(result.message); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
  protected async disableTwoFactor(): Promise<void> {
    if (!this.codeForm.controls.password.value) { this.codeForm.controls.password.markAsTouched(); return; }
    this.busy.set(true); this.error.set(null);
    try { const result = await this.auth.disableTwoFactor(this.codeForm.controls.password.value); await this.auth.refresh(); this.codeForm.reset(); this.message.set(result.message); this.toast.success(result.message, 'Security updated'); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
  protected async regenerateRecoveryCodes(): Promise<void> {
    if (!this.passwordOnlyForm.controls.password.value) { this.passwordOnlyForm.controls.password.markAsTouched(); return; }
    this.busy.set(true); this.error.set(null);
    try { const result = await this.auth.regenerateRecoveryCodes(this.passwordOnlyForm.controls.password.value); this.recoveryCodes.set(result.recoveryCodes); this.passwordOnlyForm.reset(); this.message.set('New recovery codes are ready. Store them somewhere safe.'); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
  protected async changePassword(): Promise<void> {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();
    if (newPassword !== confirmPassword) { this.error.set('The new passwords do not match.'); return; }
    this.busy.set(true); this.error.set(null);
    try { await this.auth.changePassword(currentPassword, newPassword); this.toast.success('Your password changed. Please sign in again.', 'Password updated'); await this.router.navigate(['/login']); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.busy.set(false); }
  }
}
