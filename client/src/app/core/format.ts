export function inr(value: number | null | undefined, decimals = false): string {
  const amount = Number(value ?? 0);
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: decimals ? 2 : 0,
    maximumFractionDigits: decimals ? 2 : 0,
  }).format(amount);
}

export function compactNumber(value: number | null | undefined): string {
  const amount = Number(value ?? 0);
  return new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 }).format(amount);
}

export function durationLabel(minutes: number | null | undefined): string {
  const total = Number(minutes ?? 0);
  if (total <= 0) return 'Flexible';
  const hours = Math.floor(total / 60);
  const mins = total % 60;
  if (hours && mins) return `${hours} hr ${mins} min`;
  if (hours) return `${hours} hr`;
  return `${mins} min`;
}

export function videoDuration(seconds: number | null | undefined): string {
  const total = Math.max(0, Math.floor(Number(seconds ?? 0)));
  return `${Math.floor(total / 60)}:${(total % 60).toString().padStart(2, '0')}`;
}

export function dateLabel(value: string | Date | null | undefined): string {
  if (!value) return '';
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }).format(date);
}

export function dateTimeLabel(value: string | Date | null | undefined): string {
  if (!value) return '';
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat('en-IN', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(date);
}

export function timeAgo(value: string | Date | null | undefined): string {
  if (!value) return '';
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  const diff = Date.now() - date.getTime();
  const minutes = Math.round(diff / 60000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} hr ago`;
  const days = Math.round(hours / 24);
  if (days < 7) return `${days} day${days === 1 ? '' : 's'} ago`;
  const weeks = Math.round(days / 7);
  if (weeks < 5) return `${weeks} week${weeks === 1 ? '' : 's'} ago`;
  return dateLabel(date);
}

export function statusLabel(value: string | null | undefined): string {
  if (!value) return '';
  return value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .trim();
}

export function statusTone(value: string | null | undefined): 'success' | 'warning' | 'danger' | 'info' | 'neutral' {
  switch ((value ?? '').toLowerCase()) {
    case 'paid':
    case 'captured':
    case 'delivered':
    case 'confirmed':
    case 'completed':
    case 'approved':
    case 'processed':
    case 'replied':
    case 'resolved':
      return 'success';
    case 'processing':
    case 'shipped':
    case 'pending':
    case 'paymentpending':
    case 'open':
    case 'read':
    case 'new':
    case 'partiallyrefunded':
      return 'warning';
    case 'cancelled':
    case 'failed':
    case 'rejected':
    case 'refunded':
      return 'danger';
    case 'authorized':
    case 'created':
      return 'info';
    default:
      return 'neutral';
  }
}

export function parseAddress(json: string | null | undefined): Record<string, string> {
  if (!json) return {};
  try {
    const raw = JSON.parse(json) as Record<string, unknown>;
    return Object.fromEntries(
      Object.entries(raw)
        .filter(([, value]) => value !== null && value !== undefined)
        .map(([key, value]) => [key, String(value)]),
    );
  } catch {
    return {};
  }
}

export function initials(name: string | null | undefined): string {
  return (name ?? '')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
}

export function todayIso(): string {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

export function addDaysIso(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

export function csvToList(value: string | null | undefined): string[] {
  return (value ?? '')
    .split(',')
    .map((part) => part.trim())
    .filter(Boolean);
}

export function fileSize(bytes: number | null | undefined): string {
  const size = Number(bytes ?? 0);
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
}
