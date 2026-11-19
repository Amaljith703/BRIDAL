import { Component, computed, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MusicService } from './core/music.service';
import { MusicGateComponent } from './shared/components/music-gate.component';
import { MusicPlayerComponent } from './shared/components/music-player.component';
import { ToastHostComponent } from './shared/components/toast-host.component';

@Component({
  imports: [RouterOutlet, ToastHostComponent, MusicGateComponent, MusicPlayerComponent],
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly music = inject(MusicService);
  protected readonly showGate = computed(() => this.music.gateOpen());
}
