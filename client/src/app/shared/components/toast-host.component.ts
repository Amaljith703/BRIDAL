import { Component, inject } from '@angular/core';
import { ToastService } from '../../core/toast.service';
import { IconComponent } from '../icon.component';

@Component({
  selector: 'app-toast-host',
  imports: [IconComponent],
  template: `
    <div class="toast-host" aria-live="polite" aria-atomic="false">
      @for (toast of toasts.toasts(); track toast.id) {
        <article class="toast" [class]="'toast toast--' + toast.tone">
          <span class="toast__icon">
            <app-icon [name]="iconFor(toast.tone)" [size]="17" />
          </span>
          <div class="toast__copy">
            <strong>{{ toast.title }}</strong>
            @if (toast.message) {
              <p>{{ toast.message }}</p>
            }
          </div>
          <button class="toast__close" type="button" (click)="toasts.dismiss(toast.id)" aria-label="Dismiss">
            <app-icon name="close" [size]="14" />
          </button>
        </article>
      }
    </div>
  `,
  styles: `
    .toast-host {
      position: fixed;
      top: 18px;
      right: 18px;
      z-index: 3000;
      display: grid;
      gap: 10px;
      width: min(380px, calc(100vw - 32px));
      pointer-events: none;
    }
    .toast {
      pointer-events: auto;
      display: grid;
      grid-template-columns: 34px minmax(0, 1fr) 26px;
      gap: 10px;
      align-items: start;
      padding: 14px 14px 14px 13px;
      border-radius: 16px;
      border: 1px solid rgba(61, 42, 47, 0.12);
      background: rgba(255, 253, 251, 0.98);
      box-shadow: 0 24px 50px -24px rgba(49, 26, 32, 0.45);
      backdrop-filter: blur(8px);
      animation: toastIn 0.3s cubic-bezier(0.22, 1, 0.36, 1);
    }
    .toast__icon {
      display: grid;
      place-items: center;
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background: rgba(142, 74, 91, 0.1);
      color: #8e4a5b;
    }
    .toast--success .toast__icon {
      background: rgba(58, 122, 90, 0.12);
      color: #2f6f4f;
    }
    .toast--error .toast__icon {
      background: rgba(170, 62, 62, 0.12);
      color: #9b3434;
    }
    .toast--warning .toast__icon {
      background: rgba(177, 120, 34, 0.14);
      color: #8a5a10;
    }
    .toast--info .toast__icon {
      background: rgba(54, 95, 140, 0.12);
      color: #355f8c;
    }
    .toast__copy {
      padding-top: 1px;
    }
    .toast__copy strong {
      display: block;
      font-size: 0.82rem;
      letter-spacing: 0.01em;
    }
    .toast__copy p {
      margin: 3px 0 0;
      font-size: 0.8rem;
      line-height: 1.45;
      color: #6f5a60;
    }
    .toast__close {
      display: grid;
      place-items: center;
      width: 26px;
      height: 26px;
      border-radius: 50%;
      color: #6f5a60;
    }
    .toast__close:hover {
      background: rgba(61, 42, 47, 0.08);
      color: #332226;
    }
    @keyframes toastIn {
      from {
        opacity: 0;
        transform: translateY(-8px) scale(0.98);
      }
      to {
        opacity: 1;
        transform: none;
      }
    }
  `,
})
export class ToastHostComponent {
  protected readonly toasts = inject(ToastService);

  protected iconFor(tone: string): string {
    switch (tone) {
      case 'success':
        return 'check';
      case 'error':
        return 'alert';
      case 'warning':
        return 'alert';
      default:
        return 'info';
    }
  }
}
