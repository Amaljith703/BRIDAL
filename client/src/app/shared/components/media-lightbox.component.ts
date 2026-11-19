import { Component, HostListener, inject, input, output } from '@angular/core';
import { MusicService } from '../../core/music.service';
import { IconComponent } from '../icon.component';
import { ImgFallbackDirective } from '../img-fallback.directive';

export interface LightboxItem {
  title: string;
  imageUrl?: string | null;
  videoUrl?: string | null;
  description?: string | null;
  meta?: string | null;
}

@Component({
  selector: 'app-media-lightbox',
  imports: [IconComponent, ImgFallbackDirective],
  template: `
    <div class="lightbox" role="dialog" aria-modal="true" [attr.aria-label]="item().title" (click)="close.emit()">
      <button type="button" class="lightbox__close" (click)="close.emit()" aria-label="Close">
        <app-icon name="close" [size]="22" />
      </button>

      <div class="lightbox__stage" (click)="$event.stopPropagation()">
        @if (item().videoUrl) {
          <video
            class="lightbox__video"
            [src]="item().videoUrl"
            [attr.poster]="item().imageUrl || null"
            controls
            autoplay
            playsinline
          ></video>
        } @else if (item().imageUrl) {
          <img class="lightbox__image" [src]="item().imageUrl" [alt]="item().title" appImgFallback />
        }
        <div class="lightbox__copy">
          @if (item().meta) {
            <span class="lightbox__meta">{{ item().meta }}</span>
          }
          <h2>{{ item().title }}</h2>
          @if (item().description) {
            <p>{{ item().description }}</p>
          }
        </div>
      </div>
    </div>
  `,
  styles: `
    .lightbox {
      position: fixed;
      inset: 0;
      z-index: 2500;
      background: rgba(20, 11, 14, 0.9);
      backdrop-filter: blur(10px);
      display: grid;
      place-items: center;
      padding: clamp(16px, 4vw, 44px);
      animation: lbFade 0.25s ease;
    }
    .lightbox__close {
      position: absolute;
      top: 20px;
      right: 20px;
      z-index: 2;
      width: 46px;
      height: 46px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      color: rgba(250, 246, 241, 0.8);
      border: 1px solid rgba(250, 246, 241, 0.2);
      background: rgba(43, 27, 32, 0.5);
    }
    .lightbox__close:hover {
      color: #fff;
      border-color: rgba(250, 246, 241, 0.5);
    }
    .lightbox__stage {
      width: min(1080px, 100%);
      max-height: calc(100vh - 64px);
      display: grid;
      grid-template-columns: minmax(0, 1.55fr) minmax(0, 1fr);
      background: #24171b;
      border-radius: 22px;
      overflow: hidden;
      border: 1px solid rgba(233, 200, 193, 0.16);
      box-shadow: 0 50px 100px -30px rgba(0, 0, 0, 0.8);
      animation: lbRise 0.35s var(--ease);
    }
    .lightbox__image,
    .lightbox__video {
      width: 100%;
      height: 100%;
      max-height: calc(100vh - 64px);
      object-fit: cover;
      background: #120c0e;
    }
    .lightbox__video {
      object-fit: contain;
    }
    .lightbox__copy {
      padding: clamp(24px, 3vw, 42px);
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
      gap: 10px;
      color: #faf6f1;
      background:
        radial-gradient(500px 240px at 100% 0%, rgba(233, 200, 193, 0.14), transparent 60%),
        #24171b;
    }
    .lightbox__meta {
      display: inline-flex;
      width: fit-content;
      font-size: 0.6rem;
      font-weight: 700;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      color: #c9a26b;
      border: 1px solid rgba(201, 162, 107, 0.35);
      border-radius: 999px;
      padding: 5px 12px;
    }
    .lightbox__copy h2 {
      color: #faf6f1;
      font-size: clamp(1.8rem, 3vw, 2.8rem);
    }
    .lightbox__copy p {
      color: rgba(250, 246, 241, 0.62);
      font-size: 0.88rem;
      line-height: 1.7;
      margin: 0;
    }
    @keyframes lbFade {
      from {
        opacity: 0;
      }
    }
    @keyframes lbRise {
      from {
        opacity: 0;
        transform: translateY(16px) scale(0.98);
      }
    }
    @media (max-width: 820px) {
      .lightbox__stage {
        grid-template-columns: 1fr;
        overflow-y: auto;
      }
      .lightbox__image,
      .lightbox__video {
        max-height: 56vh;
        object-fit: contain;
      }
      .lightbox__copy {
        justify-content: flex-start;
      }
    }
  `,
})
export class MediaLightboxComponent {
  readonly item = input.required<LightboxItem>();
  readonly close = output<void>();
  private readonly music = inject(MusicService);

  constructor() {
    this.music.pauseForVideo();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close.emit();
  }

  ngOnDestroy(): void {
    this.music.resumeAfterVideo();
  }
}
