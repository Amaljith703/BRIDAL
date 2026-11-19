import { HttpClient, HttpErrorResponse, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProblemDetails } from './models';

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly detail?: string,
    public readonly fields?: Record<string, string[]>,
    public readonly traceId?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  readonly base = '/api';

  get<T>(path: string, query?: Record<string, unknown>): Promise<T> {
    return firstValueFrom(this.http.get<T>(this.url(path), { params: this.params(query) }));
  }

  post<T>(path: string, body?: unknown): Promise<T> {
    return firstValueFrom(this.http.post<T>(this.url(path), body ?? {}));
  }

  put<T>(path: string, body?: unknown): Promise<T> {
    return firstValueFrom(this.http.put<T>(this.url(path), body ?? {}));
  }

  delete<T = void>(path: string): Promise<T> {
    return firstValueFrom(this.http.delete<T>(this.url(path)));
  }

  upload<T>(path: string, form: FormData): Promise<T> {
    return firstValueFrom(this.http.post<T>(this.url(path), form));
  }

  blob(path: string): Promise<Blob> {
    return firstValueFrom(this.http.get(this.url(path), { responseType: 'blob' }));
  }

  /** Fire-and-forget calls such as analytics. */
  ping(path: string, body?: unknown): void {
    this.http.post(this.url(path), body ?? {}, { responseType: 'text' }).subscribe({
      error: () => {},
    });
  }

  private url(path: string): string {
    if (path.startsWith('http')) return path;
    return `${this.base}/${path.replace(/^\//, '')}`;
  }

  private params(query?: Record<string, unknown>): HttpParams {
    let params = new HttpParams();
    if (!query) return params;
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null || value === '') continue;
      if (Array.isArray(value)) {
        for (const item of value) {
          if (item !== undefined && item !== null && item !== '') params = params.append(key, String(item));
        }
      } else {
        params = params.set(key, String(value));
      }
    }
    return params;
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (error instanceof HttpErrorResponse) {
    const problem = (error.error ?? {}) as ProblemDetails;
    const detail = problem.detail || problem.title || (typeof error.error === 'string' ? error.error : '');
    const fieldMessage = problem.errors
      ? Object.values(problem.errors).flat().filter(Boolean).join(' ')
      : '';
    const message =
      fieldMessage ||
      detail ||
      (error.status === 0
        ? 'We could not reach the studio server. Check your connection and try again.'
        : error.status === 429
          ? 'A little too many requests just now. Please wait a moment and try again.'
          : error.message) ||
      'Something went wrong. Please try again.';
    return new ApiError(message, error.status, detail, problem.errors, problem.traceId);
  }
  return new ApiError(error instanceof Error ? error.message : 'Something went wrong.', 0);
}

export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  setTimeout(() => URL.revokeObjectURL(url), 4000);
}
