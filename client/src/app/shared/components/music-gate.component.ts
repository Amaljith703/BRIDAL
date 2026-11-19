import { Component, inject } from '@angular/core';
import { MusicService } from '../../core/music.service';
import { SiteService } from '../../core/site.service';
import { IconComponent } from '../icon.component';

@Component({
  selector: 'app-music-gate',
  imports: [IconComponent],
  template: `
    <div class="gate" role="dialog" aria-modal="true" aria-labelledby="gate-title">
      <div class="gate__panel">
        <div class="gate__visual" aria-hidden="true">
          <div class="gate__ring gate__ring--one"></div>
          <div class="gate__ring gate__ring--two"></div>
          <div class="gate__disc">
            <span class="gate__disc-label">MA</span>
          </div>
          <div class="gate__petal gate__petal--one"></div>
          <div class="gate__petal gate__petal--two"></div>
          <div class="gate__petal gate__petal--three"></div>
        </div>

        <div class="gate__copy">
          <span class="eyebrow eyebrow--light">Welcome to {{ site.settings().businessName }}</span>
          <h2 id="gate-title">Step inside<br /><em>the studio.</em></h2>
          <p>
            Our original instrumental soundscape begins the moment you enter and follows you through the
            studio — soft, warm and lyric-free.
          </p>

          <div class="gate__actions">
            <button type="button" class="btn btn--gold" (click)="music.enterWithSound()">
              <app-icon name="volume" [size]="17" />
              Enter with sound
            </button>
            <button type="button" class="btn btn--ghost-light" (click)="music.enterQuietly()">
              Enter quietly
            </button>
          </div>

          <p class="gate__note">
            Your browser needs one tap before audio can play. You can pause, change track or mute at any
            time from the player.
          </p>

          <div class="gate__tracks">
            <app-icon name="music" [size]="15" />
            <span>{{ trackLabel() }}</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: `
    .gate {
      position: fixed;
      inset: 0;
      z-index: 2600;
      display: grid;
      place-items: center;
      padding: 20px;
      background:
        radial-gradient(1200px 600px at 15% 0%, rgba(233, 200, 193, 0.18), transparent 60%),
        rgba(24, 14, 18, 0.82);
      backdrop-filter: blur(10px);
      animation: gateFade 0.4s ease;
    }
    .gate__panel {
      display: grid;
      grid-template-columns: minmax(0, 0.86fr) minmax(0, 1.14fr);
      width: min(940px, 100%);
      max-height: min(88vh, 640px);
      overflow: auto;
      border-radius: 28px;
      background: #2b1b20;
      color: #faf6f1;
      box-shadow: 0 50px 120px -40px rgba(0, 0, 0, 0.7);
      border: 1px solid rgba(233, 200, 193, 0.18);
      animation: gateRise 0.5s cubic-bezier(0.22, 1, 0.36, 1);
    }
    .gate__visual {
      position: relative;
      min-height: 420px;
      overflow: hidden;
      background:
        radial-gradient(circle at 50% 48%, rgba(233, 200, 193, 0.3), transparent 46%),
        linear-gradient(155deg, #5e2e3c, #2b1b20 70%);
    }
    .gate__ring {
      position: absolute;
      border: 1px solid rgba(233, 200, 193, 0.3);
      border-radius: 50%;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
    }
    .gate__ring--one {
      width: 300px;
      height: 300px;
      animation: gatePulse 7s ease-in-out infinite;
    }
    .gate__ring--two {
      width: 210px;
      height: 210px;
      border-style: dashed;
      animation: gateSpin 24s linear infinite;
    }
    .gate__disc {
      position: absolute;
      left: 50%;
      top: 50%;
      width: 148px;
      height: 148px;
      transform: translate(-50%, -50%);
      border-radius: 50%;
      display: grid;
      place-items: center;
      background:
        radial-gradient(circle at 38% 32%, rgba(255, 255, 255, 0.28), transparent 40%),
        linear-gradient(150deg, #c9a26b, #8e4a5b 58%, #4c2430);
      box-shadow:
        0 24px 50px -18px rgba(0, 0, 0, 0.7),
        inset 0 0 0 10px rgba(43, 27, 32, 0.34),
        inset 0 0 0 11px rgba(250, 246, 241, 0.18);
      animation: gateBreath 6s ease-in-out infinite;
    }
    .gate__disc-label {
      font-family: var(--font-display);
      font-size: 2rem;
      letter-spacing: 0.16em;
      color: #faf6f1;
      opacity: 0.9;
    }
    .gate__petal {
      position: absolute;
      width: 12px;
      height: 12px;
      border-radius: 50% 50% 50% 0;
      background: rgba(233, 200, 193, 0.5);
      filter: blur(0.3px);
      animation: gateFloat 9s ease-in-out infinite;
    }
    .gate__petal--one {
      left: 22%;
      top: 26%;
    }
    .gate__petal--two {
      right: 18%;
      top: 60%;
      width: 8px;
      height: 8px;
      animation-delay: -3s;
    }
    .gate__petal--three {
      left: 34%;
      bottom: 18%;
      width: 6px;
      height: 6px;
      animation-delay: -6s;
    }
    .gate__copy {
      padding: clamp(28px, 4vw, 52px);
      display: flex;
      flex-direction: column;
      justify-content: center;
      gap: 12px;
    }
    .gate__copy h2 {
      color: #faf6f1;
      font-size: clamp(2.3rem, 4vw, 3.4rem);
      margin: 6px 0 4px;
    }
    .gate__copy h2 em {
      color: #e9c8c1;
      font-weight: 400;
    }
    .gate__copy p {
      color: rgba(250, 246, 241, 0.68);
      font-size: 0.9rem;
      line-height: 1.7;
      margin: 0;
      max-width: 42ch;
    }
    .gate__actions {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      margin-top: 14px;
    }
    .gate__note {
      font-size: 0.74rem !important;
      color: rgba(250, 246, 241, 0.45) !important;
      margin-top: 6px !important;
    }
    .gate__tracks {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      margin-top: 8px;
      color: rgba(201, 162, 107, 0.9);
      font-size: 0.7rem;
      letter-spacing: 0.14em;
      text-transform: uppercase;
    }
    @keyframes gateFade {
      from {
        opacity: 0;
      }
    }
    @keyframes gateRise {
      from {
        opacity: 0;
        transform: translateY(22px) scale(0.985);
      }
    }
    @keyframes gateSpin {
      to {
        transform: translate(-50%, -50%) rotate(360deg);
      }
    }
    @keyframes gatePulse {
      0%,
      100% {
        transform: translate(-50%, -50%) scale(1);
        opacity: 0.5;
      }
      50% {
        transform: translate(-50%, -50%) scale(1.06);
        opacity: 0.9;
      }
    }
    @keyframes gateBreath {
      0%,
      100% {
        transform: translate(-50%, -50%) scale(1);
      }
      50% {
        transform: translate(-50%, -50%) scale(1.035);
      }
    }
    @keyframes gateFloat {
      0%,
      100% {
        transform: translateY(0) rotate(0);
        opacity: 0.35;
      }
      50% {
        transform: translateY(-18px) rotate(50deg);
        opacity: 0.8;
      }
    }
    @media (max-width: 820px) {
      .gate__panel {
        grid-template-columns: 1fr;
        max-height: 92vh;
      }
      .gate__visual {
        min-height: 190px;
      }
      .gate__disc {
        width: 104px;
        height: 104px;
      }
      .gate__ring--one {
        width: 230px;
        height: 230px;
      }
      .gate__ring--two {
        width: 160px;
        height: 160px;
      }
      .gate__copy {
        padding: 28px 24px 34px;
      }
    }
  `,
})
export class MusicGateComponent {
  protected readonly music = inject(MusicService);
  protected readonly site = inject(SiteService);

  protected trackLabel(): string {
    const tracks = this.music.tracks();
    if (!tracks.length) return 'Original studio instrumentals';
    return `${tracks.length} original instrumentals · ${tracks.map((t) => t.title).join(' · ')}`;
  }
}
