import { Component, computed, input } from '@angular/core';

interface IconDef {
  paths: string[];
  fill?: boolean;
  viewBox?: string;
}

const ICONS: Record<string, IconDef> = {
  menu: { paths: ['M4 7h16', 'M4 12h16', 'M4 17h16'] },
  close: { paths: ['M6 6l12 12', 'M18 6L6 18'] },
  x: { paths: ['M6 6l12 12', 'M18 6L6 18'] },
  plus: { paths: ['M12 5v14', 'M5 12h14'] },
  minus: { paths: ['M5 12h14'] },
  'arrow-right': { paths: ['M4 12h16', 'M13 5l7 7-7 7'] },
  'arrow-left': { paths: ['M20 12H4', 'M11 5l-7 7 7 7'] },
  'arrow-up-right': { paths: ['M7 17L17 7', 'M8 7h9v9'] },
  'chevron-down': { paths: ['M6 9l6 6 6-6'] },
  'chevron-left': { paths: ['M15 5l-7 7 7 7'] },
  'chevron-right': { paths: ['M9 5l7 7-7 7'] },
  'chevron-up': { paths: ['M6 15l6-6 6 6'] },
  cart: { paths: ['M6 8h12l1 12H5L6 8Z', 'M9 8V6a3 3 0 0 1 6 0v2'] },
  bag: { paths: ['M5 8h14l-1 12H6L5 8Z', 'M9 8V6a3 3 0 0 1 6 0v2'] },
  user: { paths: ['M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M4 21a8 8 0 0 1 16 0'] },
  users: { paths: ['M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M2 21a7 7 0 0 1 14 0', 'M16 3.5a4 4 0 0 1 0 7.5', 'M18 14a7 7 0 0 1 4 7'] },
  search: { paths: ['M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16Z', 'M21 21l-4.35-4.35'] },
  play: { paths: ['M8 5.5v13l11-6.5-11-6.5Z'], fill: true },
  pause: { paths: ['M8 4h3v16H8z', 'M13 4h3v16h-3z'], fill: true },
  volume: { paths: ['M4 10v4h4l5 4V6L8 10H4Z', 'M16 9a4 4 0 0 1 0 6', 'M18.5 6.5a8 8 0 0 1 0 11'] },
  'volume-x': { paths: ['M4 10v4h4l5 4V6L8 10H4Z', 'M17 10l4 4', 'M21 10l-4 4'] },
  music: { paths: ['M9 18V5l11-2v13', 'M9 18a3 3 0 1 1-3-3h3', 'M20 16a3 3 0 1 1-3-3h3'] },
  star: { paths: ['M12 2.8l2.7 5.5 6 .9-4.35 4.25 1.03 6-5.38-2.83L6.62 19.5l1.03-6L3.3 9.2l6-.9L12 2.8Z'], fill: true },
  heart: { paths: ['M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21.2l7.8-7.8 1-1a5.5 5.5 0 0 0 0-7.8Z'] },
  check: { paths: ['M4 12.5l5 5L20 6.5'] },
  'map-pin': { paths: ['M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0Z', 'M12 12.5a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z'] },
  phone: { paths: ['M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1 1 .4 2 .7 2.9a2 2 0 0 1-.5 2.1L8.1 10a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.5c.9.3 1.9.6 2.9.7a2 2 0 0 1 1.6 2Z'] },
  mail: { paths: ['M3 5h18v14H3z', 'M3 6l9 7 9-7'] },
  clock: { paths: ['M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20Z', 'M12 6v6l4 2'] },
  calendar: { paths: ['M4 6h16v15H4z', 'M8 3v5', 'M16 3v5', 'M4 10h16'] },
  instagram: { paths: ['M7 3h10a4 4 0 0 1 4 4v10a4 4 0 0 1-4 4H7a4 4 0 0 1-4-4V7a4 4 0 0 1 4-4Z', 'M12 16a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M17.5 6.5h.01'] },
  facebook: { paths: ['M14 21v-7h3l.5-3H14V9c0-1 .3-1.7 1.8-1.7H18V4.2A24 24 0 0 0 15.4 4C12.8 4 11 5.6 11 8.5V11H8v3h3v7'] },
  youtube: { paths: ['M22 12s0-3.4-.4-5a2.7 2.7 0 0 0-1.9-1.9C17.9 4.6 12 4.6 12 4.6s-5.9 0-7.7.5A2.7 2.7 0 0 0 2.4 7C2 8.6 2 12 2 12s0 3.4.4 5a2.7 2.7 0 0 0 1.9 1.9c1.8.5 7.7.5 7.7.5s5.9 0 7.7-.5a2.7 2.7 0 0 0 1.9-1.9c.4-1.6.4-5 .4-5Z', 'M10 9l5 3-5 3V9Z'] },
  pinterest: { paths: ['M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20Z', 'M9 21c1-2.5 2-6.5 2-6.5', 'M8.5 11.5c0-2.5 2-4.5 4.5-4.5 2.2 0 3.8 1.4 3.8 3.5 0 2.7-1.6 4.7-3.7 4.7-1 0-1.8-.8-1.5-1.9'] },
  whatsapp: { paths: ['M21 11.5a8.5 8.5 0 0 1-12.7 7.4L3 21l2.2-5.2A8.5 8.5 0 1 1 21 11.5Z', 'M9 8.5c.3 2.5 1.8 4.4 4.2 5.3l1.3-1.3 2 1c-.2 1.4-1.3 2.2-2.6 2.1-3.7-.3-6.7-3.3-7-7 0-1.3.8-2.4 2.2-2.6l1 2-1.1 1.5'] },
  sparkles: { paths: ['M12 3l1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6L12 3Z', 'M19 14l.8 2.2L22 17l-2.2.8L19 20l-.8-2.2L16 17l2.2-.8L19 14Z', 'M5 14l.8 2.2L8 17l-2.2.8L5 20l-.8-2.2L2 17l2.2-.8L5 14Z'] },
  message: { paths: ['M21 11.5a8.5 8.5 0 0 1-12.4 7.5L3 21l2-5.6A8.5 8.5 0 1 1 21 11.5Z'] },
  send: { paths: ['M22 2L11 13', 'M22 2l-7 20-4-9-9-4 20-7Z'] },
  trash: { paths: ['M3 6h18', 'M8 6V4h8v2', 'M6 6l1 16h10l1-16', 'M10 11v6', 'M14 11v6'] },
  edit: { paths: ['M12 20h9', 'M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5Z'] },
  upload: { paths: ['M12 16V4', 'M7 9l5-5 5 5', 'M4 20h16'] },
  download: { paths: ['M12 4v12', 'M7 11l5 5 5-5', 'M4 20h16'] },
  logout: { paths: ['M9 21H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5', 'M16 17l5-5-5-5', 'M21 12H9'] },
  shield: { paths: ['M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z', 'M9 12l2 2 4-4'] },
  'credit-card': { paths: ['M2 5h20v14H2z', 'M2 10h20', 'M6 15h4'] },
  truck: { paths: ['M3 6h11v11H3z', 'M14 10h4l3 3v4h-7', 'M6.5 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z', 'M18 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z'] },
  package: { paths: ['M21 8l-9-5-9 5 9 5 9-5Z', 'M3 8v8l9 5 9-5V8', 'M12 13v8'] },
  grid: { paths: ['M3 3h7v7H3z', 'M14 3h7v7h-7z', 'M3 14h7v7H3z', 'M14 14h7v7h-7z'] },
  chart: { paths: ['M4 20V10', 'M10 20V4', 'M16 20v-7', 'M22 20H2'] },
  image: { paths: ['M3 3h18v18H3z', 'M8.5 10a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z', 'M21 15l-5-5L5 21'] },
  settings: { paths: ['M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z', 'M19.4 15a1.7 1.7 0 0 0 .34 1.87l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 1.55V21a2 2 0 1 1-4 0v-.09A1.7 1.7 0 0 0 9 19.4a1.7 1.7 0 0 0-1.87.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.7 1.7 0 0 0 4.6 15a1.7 1.7 0 0 0-1.55-1H3a2 2 0 1 1 0-4h.09A1.7 1.7 0 0 0 4.6 9a1.7 1.7 0 0 0-.34-1.87l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.7 1.7 0 0 0 9 4.6h.09A1.7 1.7 0 0 0 10 3.05V3a2 2 0 1 1 4 0v.09A1.7 1.7 0 0 0 15 4.6a1.7 1.7 0 0 0 1.87-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.7 1.7 0 0 0 19.4 9v.09a1.7 1.7 0 0 0 1.55 1H21a2 2 0 1 1 0 4h-.09a1.7 1.7 0 0 0-1.51 1Z'] },
  database: { paths: ['M12 8c5 0 9-1.1 9-2.5S17 3 12 3 3 4.1 3 5.5 7 8 12 8Z', 'M3 5.5v13C3 19.9 7 21 12 21s9-1.1 9-2.5v-13', 'M3 12c0 1.4 4 2.5 9 2.5s9-1.1 9-2.5'] },
  'file-text': { paths: ['M6 2h8l4 4v16H6z', 'M14 2v6h6', 'M9 13h6', 'M9 17h6', 'M9 9h2'] },
  layers: { paths: ['M12 2L2 7l10 5 10-5-10-5Z', 'M2 12l10 5 10-5', 'M2 17l10 5 10-5'] },
  home: { paths: ['M3 10l9-7 9 7', 'M5 9v12h14V9', 'M9 21v-7h6v7'] },
  scissors: { paths: ['M6 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z', 'M6 21a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z', 'M20 4L8.1 15.9', 'M14.5 14.5L20 20', 'M8.1 8.1L12 12'] },
  brush: { paths: ['M9.5 14.5L3 21', 'M14 3l7 7-8.5 8.5a4 4 0 0 1-5.7-5.7L14 3Z', 'M13 4l7 7'] },
  gem: { paths: ['M6 3h12l4 6-10 12L2 9l4-6Z', 'M2 9h20', 'M12 21L8 9l4-6 4 6-4 12Z'] },
  eye: { paths: ['M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z', 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z'] },
  lock: { paths: ['M5 11h14v10H5z', 'M8 11V7a4 4 0 0 1 8 0v4', 'M12 15v2'] },
  external: { paths: ['M14 4h6v6', 'M20 4L10 14', 'M20 14v6H4V4h6'] },
  filter: { paths: ['M3 5h18l-7 8v6l-4 2v-8L3 5Z'] },
  sort: { paths: ['M8 4v16', 'M4 8l4-4 4 4', 'M16 4v16', 'M12 16l4 4 4-4'] },
  alert: { paths: ['M12 3L2 21h20L12 3Z', 'M12 9v5', 'M12 18h.01'] },
  info: { paths: ['M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20Z', 'M12 11v6', 'M12 7h.01'] },
  refresh: { paths: ['M21 12a9 9 0 0 1-15.5 6.2L3 16', 'M3 12a9 9 0 0 1 15.5-6.2L21 8', 'M21 3v5h-5', 'M3 21v-5h5'] },
  copy: { paths: ['M9 9h12v12H9z', 'M5 15H3V3h12v2'] },
  qr: { paths: ['M3 3h7v7H3z', 'M14 3h7v7h-7z', 'M3 14h7v7H3z', 'M14 14h3v3h-3z', 'M18 18h3v3h-3z', 'M14 20h1', 'M20 14h1'] },
  bot: { paths: ['M5 8h14v11H5z', 'M12 4v4', 'M9 13h.01', 'M15 13h.01', 'M9 17h6', 'M3 12v3', 'M21 12v3'] },
  wand: { paths: ['M15 4V2', 'M15 10V8', 'M11.5 6.5H9.5', 'M20.5 6.5h-2', 'M13 8l-9 9 3 3 9-9', 'M18.5 3.5l1.5 1.5'] },
  book: { paths: ['M4 19.5A2.5 2.5 0 0 1 6.5 17H20', 'M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2Z'] },
  receipt: { paths: ['M5 2h14v20l-3-2-2 2-2-2-2 2-2-2-3 2V2Z', 'M9 7h6', 'M9 11h6', 'M9 15h4'] },
  ticket: { paths: ['M3 8a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v2a2 2 0 0 0 0 4v2a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-4V8Z', 'M13 6v12'] },
  tag: { paths: ['M20.6 13.4L12 22l-9-9V3h10l7.6 7.6a2 2 0 0 1 0 2.8Z', 'M7.5 7.5h.01'] },
  gift: { paths: ['M4 11h16v10H4z', 'M2 7h20v4H2z', 'M12 7v14', 'M12 7S9.5 2 7 4s2 3 5 3Z', 'M12 7s2.5-5 5-3-2 3-5 3Z'] },
  verified: { paths: ['M12 2l2.4 2.4 3.3-.6.6 3.3L21 9.5 19.5 12 21 14.5l-2.7 2.4-.6 3.3-3.3-.6L12 22l-2.4-2.4-3.3.6-.6-3.3L3 14.5 4.5 12 3 9.5l2.7-2.4.6-3.3 3.3.6L12 2Z', 'M9 12l2 2 4-4'] },
  razorpay: { paths: ['M4 20L12 4h4l-4 8h4l-4 8H4Z'] },
  upi: { paths: ['M3 12h4l2-5 3 10 2-5h7'] },
  leaf: { paths: ['M20 4C10 4 4 9 4 16c0 2 1 4 2 4 8 0 14-6 14-16Z', 'M4 20c4-6 8-9 14-12'] },
  globe: { paths: ['M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20Z', 'M2 12h20', 'M12 2a15 15 0 0 1 0 20 15 15 0 0 1 0-20Z'] },
  'map': { paths: ['M9 18l-6 3V6l6-3 6 3 6-3v15l-6 3-6-3Z', 'M9 3v15', 'M15 6v15'] },
};

export type IconName = keyof typeof ICONS;

@Component({
  selector: 'app-icon',
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      [attr.viewBox]="icon().viewBox ?? '0 0 24 24'"
      [attr.fill]="icon().fill ? 'currentColor' : 'none'"
      [attr.stroke]="icon().fill ? 'none' : 'currentColor'"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      @for (path of icon().paths; track $index) {
        <path [attr.d]="path" />
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      line-height: 0;
      flex-shrink: 0;
    }
    svg {
      display: block;
    }
  `,
})
export class IconComponent {
  readonly name = input<string>('sparkles');
  readonly size = input<number>(18);
  readonly strokeWidth = input<number>(1.6);
  readonly icon = computed<IconDef>(() => ICONS[this.name()] ?? ICONS['sparkles']);
}
