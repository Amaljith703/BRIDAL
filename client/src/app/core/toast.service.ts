import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  tone: ToastTone;
  title: string;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private sequence = 0;

  success(message: string, title = 'Done') {
    this.push('success', title, message);
  }

  error(message: string, title = 'Something went wrong') {
    this.push('error', title, message);
  }

  info(message: string, title = 'Good to know') {
    this.push('info', title, message);
  }

  warning(message: string, title = 'A small heads-up') {
    this.push('warning', title, message);
  }

  dismiss(id: number) {
    this.toasts.update((items) => items.filter((item) => item.id !== id));
  }

  private push(tone: ToastTone, title: string, message?: string) {
    const toast: Toast = { id: ++this.sequence, tone, title, message };
    this.toasts.update((items) => [...items.slice(-3), toast]);
    setTimeout(() => this.dismiss(toast.id), tone === 'error' ? 7000 : 4800);
  }
}
