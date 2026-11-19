import {
  Component,
  ElementRef,
  HostListener,
  Input,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { IconComponent } from '../icon.component';
import { ScrollModeService } from '../../core/scroll-mode.service';

@Component({
  selector: 'app-horizontal-rail',
  imports: [IconComponent],
  template: `
    <div class="rail" [class.is-dragging]="dragging()" [class.is-standard]="scrollMode.standard()">
      <div
        class="rail__track"
        #track
        (scroll)="onScroll()"
        (pointerdown)="startDrag($event)"
        (pointermove)="moveDrag($event)"
        (pointerup)="endDrag()"
        (pointercancel)="endDrag()"
        (click)="suppressClick($event)"
      >
        <ng-content />
      </div>

      <button
        type="button"
        class="rail__arrow rail__arrow--prev"
        [class.is-hidden]="atStart()"
        (click)="scrollBy(-1)"
        [attr.aria-label]="'Scroll ' + label + ' backwards'"
      >
        <app-icon name="chevron-left" [size]="19" />
      </button>
      <button
        type="button"
        class="rail__arrow rail__arrow--next"
        [class.is-hidden]="atEnd()"
        (click)="scrollBy(1)"
        [attr.aria-label]="'Scroll ' + label + ' forwards'"
      >
        <app-icon name="chevron-right" [size]="19" />
      </button>
    </div>
    <div class="rail__progress" aria-hidden="true"><span [style.width.%]="progress()"></span></div>
  `,
  styles: `
    :host {
      display: block;
    }
    .rail {
      position: relative;
      margin-inline: calc(var(--rail-bleed, 0px) * -1);
    }
    .rail__track {
      display: flex;
      gap: clamp(14px, 2vw, 26px);
      overflow-x: auto;
      overflow-y: hidden;
      scroll-snap-type: x mandatory;
      scrollbar-width: none;
      -ms-overflow-style: none;
      padding-inline: var(--rail-pad, 0px);
      padding-block: 6px 20px;
      scroll-padding-inline: var(--rail-pad, 0px);
      cursor: grab;
      overscroll-behavior-x: contain;
    }
    .rail__track::-webkit-scrollbar {
      display: none;
    }
    .rail.is-dragging .rail__track {
      cursor: grabbing;
      scroll-snap-type: none;
      user-select: none;
    }
    .rail.is-standard .rail__track {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(min(100%, 260px), 1fr));
      overflow: visible;
      scroll-snap-type: none;
      cursor: default;
    }
    .rail.is-standard .rail__arrow,
    .rail.is-standard + .rail__progress {
      display: none;
    }
    .rail__arrow {
      position: absolute;
      top: 50%;
      transform: translateY(calc(-50% - 10px));
      z-index: 3;
      width: 46px;
      height: 46px;
      border-radius: 50%;
      display: grid;
      place-items: center;
      background: rgba(255, 253, 251, 0.94);
      color: #332226;
      border: 1px solid rgba(61, 42, 47, 0.14);
      box-shadow: 0 16px 30px -18px rgba(49, 26, 32, 0.45);
      backdrop-filter: blur(6px);
      transition:
        opacity 0.25s var(--ease),
        transform 0.25s var(--ease),
        background 0.25s var(--ease);
    }
    .rail__arrow:hover {
      background: #8e4a5b;
      color: #fff;
      border-color: #8e4a5b;
    }
    .rail__arrow--prev {
      left: -10px;
    }
    .rail__arrow--next {
      right: -10px;
    }
    .rail__arrow.is-hidden {
      opacity: 0;
      pointer-events: none;
    }
    .rail__progress {
      height: 2px;
      background: rgba(61, 42, 47, 0.1);
      border-radius: 2px;
      overflow: hidden;
      margin-top: 2px;
    }
    .rail__progress span {
      display: block;
      height: 100%;
      background: #8e4a5b;
      border-radius: 2px;
      transition: width 0.2s linear;
    }
    @media (max-width: 700px) {
      .rail__arrow {
        display: none;
      }
    }
  `,
})
export class HorizontalRailComponent {
  protected readonly scrollMode = inject(ScrollModeService);
  @Input() label = 'carousel';
  @Input() railPad = '2px';

  private readonly trackRef = viewChild<ElementRef<HTMLElement>>('track');
  protected readonly progress = signal(0);
  protected readonly atStart = signal(true);
  protected readonly atEnd = signal(false);
  protected readonly dragging = signal(false);

  private down = false;
  private startX = 0;
  private startScroll = 0;
  private moved = false;

  @HostListener('window:resize')
  onResize(): void {
    this.onScroll();
  }

  ngOnDestroy(): void {
    // no observers to release; the element is owned by Angular.
  }

  protected onScroll(): void {
    const el = this.trackRef()?.nativeElement;
    if (!el) return;
    const max = el.scrollWidth - el.clientWidth;
    this.progress.set(max <= 0 ? 100 : Math.min(100, Math.max(0, (el.scrollLeft / max) * 100)));
    this.atStart.set(el.scrollLeft <= 6);
    this.atEnd.set(el.scrollLeft >= max - 6);
  }

  protected scrollBy(direction: number): void {
    const el = this.trackRef()?.nativeElement;
    if (!el) return;
    el.scrollBy({ left: direction * Math.max(280, el.clientWidth * 0.82), behavior: 'smooth' });
  }

  protected startDrag(event: PointerEvent): void {
    if (event.pointerType !== 'mouse') return;
    const el = this.trackRef()?.nativeElement;
    if (!el) return;
    this.down = true;
    this.moved = false;
    this.startX = event.clientX;
    this.startScroll = el.scrollLeft;
  }

  protected moveDrag(event: PointerEvent): void {
    if (!this.down) return;
    const el = this.trackRef()?.nativeElement;
    if (!el) return;
    const delta = event.clientX - this.startX;
    if (Math.abs(delta) > 6) {
      this.moved = true;
      this.dragging.set(true);
      el.scrollLeft = this.startScroll - delta;
    }
  }

  protected endDrag(): void {
    this.down = false;
    this.dragging.set(false);
    window.setTimeout(() => (this.moved = false), 0);
  }

  protected suppressClick(event: MouseEvent): void {
    if (!this.moved) return;
    event.preventDefault();
    event.stopPropagation();
    this.moved = false;
  }
}
