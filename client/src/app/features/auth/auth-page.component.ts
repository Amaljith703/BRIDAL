import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { toApiError } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';

type AuthMode = 'login' | 'register' | 'forgot' | 'reset' | 'confirm';

@Component({
  selector: 'app-auth-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './auth-page.component.html',
  styleUrl: './auth-page.component.scss',
})
export class AuthPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  protected readonly auth = inject(AuthService);
  readonly authMode = input<AuthMode>('login');

  protected readonly submitting = signal(false);
  protected readonly twoFactorRequired = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly heading = computed(() => ({
    login: 'Welcome back.',
    register: 'Make room for a little artistry.',
    forgot: 'A fresh start.',
    reset: 'Choose a new password.',
    confirm: 'One last little step.',
  })[this.authMode()]);
  protected readonly introduction = computed(() => ({
    login: 'Sign in to see your orders, appointments and saved details.',
    register: 'Create an account for a more personal studio experience.',
    forgot: 'We will send a secure password reset link if an account matches your email.',
    reset: 'Your password needs at least 10 characters, including upper and lower case, a number and a symbol.',
    confirm: 'We are checking the confirmation link for your email address.',
  })[this.authMode()]);

  protected readonly form = this.fb.nonNullable.group({
    firstName: [''],
    lastName: [''],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    password: ['', Validators.required],
    passwordConfirm: [''],
    twoFactorCode: [''],
    marketingOptIn: [false],
  });

  constructor() {
    effect(() => this.configureValidation(this.authMode()));
    effect(() => {
      if (this.authMode() === 'confirm' && !this.message() && !this.error()) void this.confirmEmail();
    });
  }

  protected async submit(): Promise<void> {
    this.error.set(null);
    this.message.set(null);
    const mode = this.authMode();
    const value = this.form.getRawValue();
    this.form.markAllAsTouched();

    if (this.form.invalid) return;
    if (mode === 'register' && (!value.firstName.trim() || !value.lastName.trim())) {
      this.error.set('Add your first and last name to continue.');
      return;
    }
    if ((mode === 'register' || mode === 'reset') && value.password !== value.passwordConfirm) {
      this.error.set('The two passwords do not match.');
      return;
    }

    this.submitting.set(true);
    try {
      if (mode === 'login') {
        const response = await this.auth.login({
          email: value.email.trim(),
          password: value.password,
          twoFactorCode: this.twoFactorRequired() ? value.twoFactorCode.trim() : null,
          rememberMe: true,
        });
        if (response.requiresTwoFactor) {
          this.twoFactorRequired.set(true);
          this.message.set('Enter the six-digit code from your authenticator app.');
          return;
        }
        if (!response.user || !response.accessToken) throw new Error('The sign-in response was incomplete. Please try again.');
        const requested = this.route.snapshot.queryParamMap.get('returnUrl');
        const returnUrl = requested?.startsWith('/') && !requested.startsWith('//')
          ? requested
          : this.auth.isStaff() ? '/admin' : '/account';
        await this.router.navigateByUrl(returnUrl);
        return;
      }

      if (mode === 'register') {
        await this.auth.register({
          firstName: value.firstName.trim(), lastName: value.lastName.trim(),
          email: value.email.trim(), phone: value.phone.trim() || null,
          password: value.password, marketingOptIn: value.marketingOptIn,
        });
        this.toast.success('Your account is ready. Welcome to the studio.', 'Welcome');
        await this.router.navigate(['/account']);
        return;
      }

      if (mode === 'forgot') {
        const response = await this.auth.forgotPassword(value.email.trim());
        this.message.set(response.message);
        return;
      }

      if (mode === 'reset') {
        const email = this.route.snapshot.queryParamMap.get('email') ?? '';
        const token = this.route.snapshot.queryParamMap.get('token') ?? '';
        if (!email || !token) throw new Error('This reset link is incomplete. Request a fresh password reset link.');
        const response = await this.auth.resetPassword(email, token, value.password);
        this.message.set(response.message);
      }
    } catch (error) {
      this.error.set(toApiError(error).message);
    } finally {
      this.submitting.set(false);
    }
  }

  protected async resendConfirmation(): Promise<void> {
    this.error.set(null);
    try {
      const response = await this.auth.resendConfirmation();
      this.message.set(response.message);
    } catch (error) {
      this.error.set(toApiError(error).message);
    }
  }

  private async confirmEmail(): Promise<void> {
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!email || !token) {
      this.error.set('This confirmation link is incomplete. Sign in and request a fresh one.');
      return;
    }
    try {
      const response = await this.auth.confirmEmail(email, token);
      this.message.set(response.message);
    } catch (error) {
      this.error.set(toApiError(error).message);
    }
  }

  private configureValidation(mode: AuthMode): void {
    const email = this.form.controls.email;
    const password = this.form.controls.password;
    const passwordConfirm = this.form.controls.passwordConfirm;
    email.setValidators(mode === 'confirm' ? [] : [Validators.required, Validators.email]);
    password.setValidators(mode === 'login' ? [Validators.required] : mode === 'register' || mode === 'reset' ? [Validators.required, Validators.minLength(10)] : []);
    passwordConfirm.setValidators(mode === 'register' || mode === 'reset' ? [Validators.required] : []);
    email.updateValueAndValidity({ emitEvent: false });
    password.updateValueAndValidity({ emitEvent: false });
    passwordConfirm.updateValueAndValidity({ emitEvent: false });
  }
}
