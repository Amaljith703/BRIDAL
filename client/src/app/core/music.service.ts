import { Injectable, computed, inject, signal } from '@angular/core';
import { MusicTrack } from './models';
import { SiteService } from './site.service';

const PREF_KEY = 'ma_music_pref';
const VOLUME_KEY = 'ma_music_volume';
const MUTED_KEY = 'ma_music_muted';

@Injectable({ providedIn: 'root' })
export class MusicService {
  private readonly site = inject(SiteService);
  private audio: HTMLAudioElement | null = null;

  readonly tracks = computed<MusicTrack[]>(() => this.site.music());
  readonly currentIndex = signal(0);
  readonly playing = signal(false);
  readonly muted = signal(this.readBoolean(MUTED_KEY, false));
  readonly volume = signal(this.readNumber(VOLUME_KEY, (this.site.settings().musicVolume || 32) / 100));
  readonly position = signal(0);
  readonly duration = signal(0);
  readonly gateOpen = signal(false);
  readonly needsGesture = signal(false);
  readonly initialized = signal(false);
  readonly enabled = computed(() => this.site.settings().musicEnabled && this.tracks().length > 0);
  readonly currentTrack = computed<MusicTrack | null>(() => {
    const list = this.tracks();
    if (!list.length) return null;
    return list[Math.min(this.currentIndex(), list.length - 1)];
  });
  readonly progress = computed(() => {
    const total = this.duration();
    return total > 0 ? Math.min(100, (this.position() / total) * 100) : 0;
  });
  readonly volumePercent = computed(() => Math.round(this.volume() * 100));

  private wasPlayingBeforePause = false;

  init(): void {
    if (this.initialized()) return;
    this.initialized.set(true);
    if (!this.enabled()) return;
    const defaultTrack = this.tracks().find((track) => track.isDefault);
    if (defaultTrack) {
      const index = this.tracks().findIndex((track) => track.id === defaultTrack.id);
      if (index >= 0) this.currentIndex.set(index);
    }
    const pref = this.readPref();
    if (pref === null) {
      this.gateOpen.set(true);
      return;
    }
    if (pref === 'on') void this.play();
  }

  enterWithSound(): void {
    this.writePref('on');
    this.gateOpen.set(false);
    this.needsGesture.set(false);
    void this.play();
  }

  enterQuietly(): void {
    this.writePref('off');
    this.gateOpen.set(false);
    this.pause();
  }

  async play(): Promise<void> {
    if (!this.enabled()) return;
    const audio = this.ensureAudio();
    if (!audio) return;
    try {
      await audio.play();
      this.playing.set(true);
      this.needsGesture.set(false);
      this.writePref('on');
    } catch {
      this.playing.set(false);
      this.needsGesture.set(true);
      this.writePref('on');
    }
  }

  pause(): void {
    this.audio?.pause();
    this.playing.set(false);
  }

  toggle(): void {
    if (this.playing()) this.pause();
    else void this.play();
  }

  next(): void {
    const list = this.tracks();
    if (!list.length) return;
    this.select((this.currentIndex() + 1) % list.length);
  }

  previous(): void {
    const list = this.tracks();
    if (!list.length) return;
    this.select((this.currentIndex() - 1 + list.length) % list.length);
  }

  select(index: number): void {
    const list = this.tracks();
    if (index < 0 || index >= list.length) return;
    const wasPlaying = this.playing();
    this.currentIndex.set(index);
    this.position.set(0);
    this.duration.set(0);
    if (this.audio) {
      this.audio.src = list[index].url;
      this.audio.load();
    }
    if (wasPlaying) void this.play();
  }

  seek(percent: number): void {
    const audio = this.audio;
    if (!audio || !Number.isFinite(audio.duration)) return;
    audio.currentTime = Math.max(0, Math.min(1, percent)) * audio.duration;
  }

  setVolume(value: number): void {
    const next = Math.max(0, Math.min(1, value));
    this.volume.set(next);
    if (this.audio) this.audio.volume = next;
    this.muted.set(next === 0);
    this.writeNumber(VOLUME_KEY, next);
    this.writeBoolean(MUTED_KEY, next === 0);
  }

  toggleMute(): void {
    const next = !this.muted();
    this.muted.set(next);
    if (this.audio) this.audio.muted = next;
    this.writeBoolean(MUTED_KEY, next);
  }

  pauseForVideo(): void {
    this.wasPlayingBeforePause = this.playing();
    if (this.playing()) this.pause();
  }

  resumeAfterVideo(): void {
    if (this.wasPlayingBeforePause) void this.play();
    this.wasPlayingBeforePause = false;
  }

  formatTime(seconds: number): string {
    if (!Number.isFinite(seconds) || seconds < 0) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  private ensureAudio(): HTMLAudioElement | null {
    if (typeof Audio === 'undefined') return null;
    if (!this.audio) {
      this.audio = new Audio();
      this.audio.loop = true;
      this.audio.preload = 'metadata';
      this.audio.volume = this.volume();
      this.audio.muted = this.muted();
      this.audio.addEventListener('play', () => this.playing.set(true));
      this.audio.addEventListener('pause', () => this.playing.set(false));
      this.audio.addEventListener('timeupdate', () => this.position.set(this.audio?.currentTime ?? 0));
      this.audio.addEventListener('loadedmetadata', () => this.duration.set(this.audio?.duration ?? 0));
      this.audio.addEventListener('durationchange', () => this.duration.set(this.audio?.duration ?? 0));
      this.audio.addEventListener('volumechange', () => {
        if (!this.audio) return;
        this.volume.set(this.audio.volume);
        this.muted.set(this.audio.muted);
      });
      this.audio.addEventListener('error', () => this.playing.set(false));
    }
    const track = this.currentTrack();
    if (track && this.audio.src !== new URL(track.url, window.location.href).href) {
      this.audio.src = track.url;
    }
    return this.audio;
  }

  private readPref(): 'on' | 'off' | null {
    try {
      const value = localStorage.getItem(PREF_KEY);
      return value === 'on' || value === 'off' ? value : null;
    } catch {
      return null;
    }
  }

  private writePref(value: 'on' | 'off'): void {
    try {
      localStorage.setItem(PREF_KEY, value);
    } catch {
      // ignore
    }
  }

  private readNumber(key: string, fallback: number): number {
    try {
      const raw = localStorage.getItem(key);
      const value = raw === null ? NaN : Number(raw);
      return Number.isFinite(value) ? Math.max(0, Math.min(1, value)) : fallback;
    } catch {
      return fallback;
    }
  }

  private readBoolean(key: string, fallback: boolean): boolean {
    try {
      const raw = localStorage.getItem(key);
      return raw === null ? fallback : raw === 'true';
    } catch {
      return fallback;
    }
  }

  private writeNumber(key: string, value: number): void {
    try {
      localStorage.setItem(key, String(value));
    } catch {
      // ignore
    }
  }

  private writeBoolean(key: string, value: boolean): void {
    try {
      localStorage.setItem(key, String(value));
    } catch {
      // ignore
    }
  }
}
