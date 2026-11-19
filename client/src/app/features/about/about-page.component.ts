import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ContentPage } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { PageHeroComponent } from '../../shared/components/page-hero.component';
import { IconComponent } from '../../shared/icon.component';
import { ImgFallbackDirective } from '../../shared/img-fallback.directive';
import { SafeHtmlPipe } from '../../shared/pipes';
import { RevealDirective } from '../../shared/reveal.directive';

@Component({
  selector: 'app-about-page',
  imports: [RouterLink, PageHeroComponent, IconComponent, ImgFallbackDirective, SafeHtmlPipe, RevealDirective],
  templateUrl: './about-page.component.html',
  styleUrl: './about-page.component.scss',
})
export class AboutPageComponent implements OnInit {
  private readonly site = inject(SiteService);

  protected readonly page = signal<ContentPage | null>(null);
  protected readonly loaded = signal(false);

  protected readonly intro = computed(() => this.page()?.sections.find((section) => section.key === 'intro') ?? null);
  protected readonly values = computed(() => this.page()?.sections.find((section) => section.key === 'values') ?? null);

  protected readonly team = [
    {
      name: 'Aarohi Sen',
      role: 'Founder & lead artist',
      copy: 'Bridal specialist with twelve years behind the chair and an obsession with skin that photographs like skin.',
      image: '/media/seed/hero-1.jpg',
    },
    {
      name: 'Meera Kulkarni',
      role: 'Senior artist · editorial',
      copy: 'Editorial and campaign work, with a soft spot for graphic liner and colour that surprises.',
      image: '/media/seed/hero-2.jpg',
    },
    {
      name: 'Rhea D’Souza',
      role: 'Artist · soft glam & lessons',
      copy: 'The calm voice in the lesson chair — she will decode your kit and make the five-minute face make sense.',
      image: '/media/seed/hero-3.jpg',
    },
    {
      name: 'Naina Thomas',
      role: 'Studio manager',
      copy: 'The person who keeps the dates honest, the kit stocked and the coffee warm.',
      image: '/media/seed/testimonial-4.jpg',
    },
  ];

  protected readonly pillars = [
    {
      icon: 'leaf',
      title: 'Skin first',
      copy: 'Barrier care, patch tests and products chosen for Indian skin tones and climate. The look only works if the skin is happy.',
    },
    {
      icon: 'brush',
      title: 'Artist-led, always',
      copy: 'Every booking is styled by a senior artist. No hand-offs, no rushed chairs, no rotating trainees on your wedding morning.',
    },
    {
      icon: 'gem',
      title: 'Quiet luxury',
      copy: 'Soft textures, considered colour and a finish that photographs beautifully without walking into the room before you.',
    },
    {
      icon: 'heart',
      title: 'A room that feels safe',
      copy: 'Warm light, honest conversation and zero pressure to look like anyone other than yourself.',
    },
  ];

  protected readonly numbers = [
    { value: '2014', label: 'Studio opened' },
    { value: '600+', label: 'Brides styled' },
    { value: '40+', label: 'Editorial shoots' },
    { value: '4', label: 'Artists on the floor' },
  ];

  async ngOnInit(): Promise<void> {
    try {
      this.page.set(await this.site.page('about'));
    } catch {
      this.page.set(null);
    } finally {
      this.loaded.set(true);
    }
  }
}
