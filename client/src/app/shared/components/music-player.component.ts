import { Component, inject, signal } from '@angular/core';
import { MusicService } from '../../core/music.service';
import { IconComponent } from '../icon.component';

@Component({
  selector: 'app-music-player',
  imports: [IconComponent],
  template: `
    <aside class="player" [class.player--open]="open()" aria-label="Studio music player">
      @if (open()) {
        <div class="player__panel">
          <header class="player__head">
            <span class="player__eyebrow"><app-icon name="music" [size]="13" /> Studio soundscape</span>
            <button type="button" class="player__icon" (click)="open.set(false)" aria-label="Collapse player">
              <app-icon name="chevron-down" [size]="16" />
            </button>
          </header>

          <div class="player__now">
            <div class="player__disc" [class.is-spinning]="music.playing()">
              <span>MA</span>
            </div>
            <div class="player__meta">
              <strong>{{ music.currentTrack()?.title ?? 'Studio soundscape' }}</strong>
              <span>{{ music.currentTrack()?.artist ?? 'Makeup Art Studio' }}</span>
            </div>
            <button type="button" class="player__mute" (click)="music.toggleMute()" [attr.aria-label]="music.muted() ? 'Unmute' : 'Mute'">
              <app-icon [name]="music.muted() ? 'volume-x' : 'volume'" [size]="16" />
            </button>
          </div>

          <div class="player__timeline">
            <input
              type="range"
              min="0"
              max="100"
              step="0.5"
              [value]="music.progress()"
              (input)="seek($event)"
              aria-label="Track position"
            />
            <div class="player__time">
              <span>{{ music.formatTime(music.position()) }}</span>
              <span>{{ music.formatTime(music.duration() || music.currentTrack()?.durationSeconds || 0) }}</span>
            </div>
          </div>

          <div class="player__controls">
            <button type="button" class="player__skip" (click)="music.previous()" aria-label="Previous track">
              <app-icon name="arrow-left" [size]="17" />
            </button>
            <button type="button" class="player__play" (click)="music.toggle()" [attr.aria-label]="music.playing() ? 'Pause music' : 'Play music'">
              <app-icon [name]="music.playing() ? 'pause' : 'play'" [size]="20" />
            </button>
            <button type="button" class="player__skip" (click)="music.next()" aria-label="Next track">
              <app-icon name="arrow-right" [size]="17" />
            </button>
          </div>

          <div class="player__volume">
            <app-icon [name]="music.muted() ? 'volume-x' : 'volume'" [size]="14" />
            <input
              type="range"
              min="0"
              max="100"
              step="1"
              [value]="music.volumePercent()"
              (input)="volume($event)"
              aria-label="Volume"
            />
            <span>{{ music.volumePercent() }}%</span>
          </div>

          <div class="player__list">
            @for (track of music.tracks(); track track.id; let i = $index) {
              <button
                type="button"
                class="player__track"
                [class.is-current]="i === music.currentIndex()"
                (click)="music.select(i)"
              >
                <span class="player__track-index">{{ i + 1 }}</span>
                <span class="player__track-copy">
                  <strong>{{ track.title }}</strong>
                  <span>{{ track.artist }} · {{ music.formatTime(track.durationSeconds) }}</span>
                </span>
                @if (i === music.currentIndex() && music.playing()) {
                  <span class="player__equalizer" aria-hidden="true"><i></i><i></i><i></i></span>
                }
              </button>
            }
          </div>

          <p class="player__credit">Original instrumentals, composed for the studio. No lyrics.</p>
        </div>
      }

      @if (!open()) {
        <button type="button" class="player__pill" (click)="open.set(true)" aria-label="Open music player">
          <span class="player__disc player__disc--small" [class.is-spinning]="music.playing()"><span>MA</span></span>
          <span class="player__pill-copy">
            <span class="player__pill-eyebrow">Studio sound</span>
            <strong>{{ music.currentTrack()?.title ?? 'Instrumental' }}</strong>
          </span>
          <span class="player__pill-bars" [class.is-playing]="music.playing()" aria-hidden="true"><i></i><i></i><i></i><i></i></span>
          <span
            class="player__pill-button"
            role="button"
            tabindex="0"
            (click)="toggle($event)"
            (keydown.enter)="toggle($event)"
            [attr.aria-label]="music.playing() ? 'Pause music' : 'Play music'"
          >
            <app-icon [name]="music.playing() ? 'pause' : 'play'" [size]="14" />
          </span>
        </button>
      }
    </aside>
  `,
  styles: `
    .player {
      position: fixed;
      left: 18px;
      bottom: 18px;
      z-index: 1400;
      max-width: calc(100vw - 36px);
      font-family: var(--font-body);
    }
    .player__pill {
      display: flex;
      align-items: center;
      gap: 11px;
      padding: 8px 10px 8px 9px;
      border-radius: 999px;
      background: rgba(43, 27, 32, 0.94);
      color: #faf6f1;
      border: 1px solid rgba(233, 200, 193, 0.25);
      box-shadow: 0 20px 45px -22px rgba(20, 10, 14, 0.75);
      backdrop-filter: blur(12px);
      transition: transform 0.3s var(--ease), box-shadow 0.3s var(--ease);
      max-width: min(340px, calc(100vw - 36px));
    }
    .player__pill:hover {
      transform: translateY(-2px);
      box-shadow: 0 26px 55px -22px rgba(20, 10, 14, 0.8);
    }
    .player__pill-copy {
      display: grid;
      gap: 1px;
      min-width: 0;
      text-align: left;
    }
    .player__pill-eyebrow {
      font-size: 0.55rem;
      letter-spacing: 0.18em;
      text-transform: uppercase;
      color: rgba(233, 200, 193, 0.65);
    }
    .player__pill-copy strong {
      font-size: 0.76rem;
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 150px;
    }
    .player__pill-bars {
      display: flex;
      align-items: flex-end;
      gap: 2px;
      height: 13px;
      width: 16px;
      opacity: 0.45;
    }
    .player__pill-bars i {
      width: 2px;
      height: 100%;
      background: #e9c8c1;
      border-radius: 2px;
      transform-origin: bottom;
    }
    .player__pill-bars.is-playing {
      opacity: 1;
    }
    .player__pill-bars.is-playing i {
      animation: bars 1s ease-in-out infinite;
    }
    .player__pill-bars.is-playing i:nth-child(2) {
      animation-delay: 0.18s;
    }
    .player__pill-bars.is-playing i:nth-child(3) {
      animation-delay: 0.36s;
    }
    .player__pill-bars.is-playing i:nth-child(4) {
      animation-delay: 0.54s;
    }
    .player__pill-button {
      display: grid;
      place-items: center;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: #e9c8c1;
      color: #2b1b20;
      flex-shrink: 0;
      cursor: pointer;
    }
    .player__disc {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      display: grid;
      place-items: center;
      background:
        radial-gradient(circle at 38% 30%, rgba(255, 255, 255, 0.22), transparent 42%),
        linear-gradient(150deg, #c9a26b, #8e4a5b 60%, #3a1f27);
      color: #faf6f1;
      font-family: var(--font-display);
      font-size: 0.8rem;
      letter-spacing: 0.1em;
      box-shadow: inset 0 0 0 5px rgba(43, 27, 32, 0.25);
      flex-shrink: 0;
    }
    .player__disc--small {
      width: 34px;
      height: 34px;
      font-size: 0.62rem;
    }
    .player__disc.is-spinning {
      animation: spinDisc 9s linear infinite;
    }
    .player__panel {
      width: min(340px, calc(100vw - 36px));
      border-radius: 22px;
      background: rgba(43, 27, 32, 0.97);
      color: #faf6f1;
      border: 1px solid rgba(233, 200, 193, 0.22);
      box-shadow: 0 36px 80px -30px rgba(15, 8, 11, 0.85);
      backdrop-filter: blur(16px);
      padding: 16px;
      animation: playerRise 0.35s var(--ease);
    }
    .player__head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      margin-bottom: 14px;
    }
    .player__eyebrow {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      font-size: 0.58rem;
      letter-spacing: 0.2em;
      text-transform: uppercase;
      color: #c9a26b;
    }
    .player__icon {
      display: grid;
      place-items: center;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      color: rgba(250, 246, 241, 0.6);
    }
    .player__icon:hover {
      background: rgba(250, 246, 241, 0.08);
      color: #faf6f1;
    }
    .player__now {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .player__meta {
      display: grid;
      gap: 2px;
      flex: 1;
      min-width: 0;
    }
    .player__meta strong {
      font-family: var(--font-display);
      font-size: 1.15rem;
      font-weight: 500;
      letter-spacing: 0.01em;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .player__meta span {
      font-size: 0.68rem;
      color: rgba(250, 246, 241, 0.5);
    }
    .player__mute {
      display: grid;
      place-items: center;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      color: rgba(250, 246, 241, 0.65);
      border: 1px solid rgba(250, 246, 241, 0.15);
    }
    .player__mute:hover {
      color: #faf6f1;
      border-color: rgba(233, 200, 193, 0.4);
    }
    .player__timeline {
      margin-top: 14px;
    }
    .player__timeline input,
    .player__volume input {
      width: 100%;
      accent-color: #e9c8c1;
      height: 4px;
      cursor: pointer;
    }
    .player__time {
      display: flex;
      justify-content: space-between;
      font-size: 0.62rem;
      color: rgba(250, 246, 241, 0.45);
      margin-top: 3px;
      font-variant-numeric: tabular-nums;
    }
    .player__controls {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 22px;
      margin: 10px 0 12px;
    }
    .player__skip {
      color: rgba(250, 246, 241, 0.75);
      display: grid;
      place-items: center;
      width: 34px;
      height: 34px;
      border-radius: 50%;
    }
    .player__skip:hover {
      color: #fff;
      background: rgba(250, 246, 241, 0.08);
    }
    .player__play {
      display: grid;
      place-items: center;
      width: 48px;
      height: 48px;
      border-radius: 50%;
      background: #e9c8c1;
      color: #2b1b20;
      box-shadow: 0 10px 24px -10px rgba(233, 200, 193, 0.7);
      transition: transform 0.25s var(--ease);
    }
    .player__play:hover {
      transform: scale(1.05);
    }
    .player__volume {
      display: flex;
      align-items: center;
      gap: 9px;
      color: rgba(250, 246, 241, 0.55);
      font-size: 0.62rem;
      font-variant-numeric: tabular-nums;
      padding-bottom: 12px;
      border-bottom: 1px solid rgba(250, 246, 241, 0.1);
    }
    .player__volume span {
      min-width: 30px;
      text-align: right;
    }
    .player__list {
      display: grid;
      gap: 2px;
      margin-top: 10px;
      max-height: 180px;
      overflow: auto;
    }
    .player__track {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 9px;
      border-radius: 10px;
      color: rgba(250, 246, 241, 0.72);
      text-align: left;
      transition: background 0.2s ease;
    }
    .player__track:hover {
      background: rgba(250, 246, 241, 0.06);
    }
    .player__track.is-current {
      background: rgba(233, 200, 193, 0.12);
      color: #faf6f1;
    }
    .player__track-index {
      font-size: 0.62rem;
      color: rgba(250, 246, 241, 0.35);
      width: 14px;
      flex-shrink: 0;
    }
    .player__track-copy {
      display: grid;
      gap: 1px;
      flex: 1;
      min-width: 0;
    }
    .player__track-copy strong {
      font-size: 0.74rem;
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .player__track-copy span {
      font-size: 0.6rem;
      color: rgba(250, 246, 241, 0.4);
    }
    .player__equalizer {
      display: flex;
      align-items: flex-end;
      gap: 2px;
      height: 12px;
      width: 14px;
    }
    .player__equalizer i {
      width: 2px;
      height: 100%;
      background: #c9a26b;
      border-radius: 2px;
      animation: bars 0.9s ease-in-out infinite;
    }
    .player__equalizer i:nth-child(2) {
      animation-delay: 0.2s;
    }
    .player__equalizer i:nth-child(3) {
      animation-delay: 0.4s;
    }
    .player__credit {
      margin: 12px 0 0;
      font-size: 0.58rem;
      color: rgba(250, 246, 241, 0.35);
      text-align: center;
    }
    @keyframes spinDisc {
      to {
        transform: rotate(360deg);
      }
    }
    @keyframes bars {
      0%,
      100% {
        transform: scaleY(0.35);
      }
      50% {
        transform: scaleY(1);
      }
    }
    @keyframes playerRise {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
    }
    @media (max-width: 560px) {
      .player {
        left: 12px;
        bottom: 12px;
      }
      .player__pill {
        max-width: calc(100vw - 24px);
      }
      .player__panel {
        width: min(340px, calc(100vw - 24px));
      }
    }
  `,
})
export class MusicPlayerComponent {
  protected readonly music = inject(MusicService);
  protected readonly open = signal(false);

  protected seek(event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.music.seek(value / 100);
  }

  protected volume(event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.music.setVolume(value / 100);
  }

  protected toggle(event: Event): void {
    event.stopPropagation();
    this.music.toggle();
  }
}
