import { Directive, ElementRef, Input, OnDestroy, OnInit, inject } from '@angular/core';
import { gsap } from 'gsap';

@Directive({ selector: '[appReveal]' })
export class RevealDirective implements OnInit, OnDestroy {
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);
  private observer: IntersectionObserver | null = null;
  private tween: gsap.core.Tween | null = null;

  @Input() revealDelay = 0;

  ngOnInit(): void {
    const element = this.el.nativeElement;
    element.classList.add('reveal');
    element.style.setProperty('--reveal-delay', `${this.revealDelay}ms`);
    if (
      typeof window === 'undefined' ||
      window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ||
      typeof IntersectionObserver === 'undefined'
    ) {
      element.classList.add('is-revealed');
      return;
    }
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            element.classList.add('is-animating');
            this.tween = gsap.fromTo(
              element,
              { autoAlpha: 0, y: 26 },
              {
                autoAlpha: 1,
                y: 0,
                duration: 0.9,
                delay: this.revealDelay / 1000,
                ease: 'power3.out',
                onComplete: () => {
                  element.classList.add('is-revealed');
                  element.classList.remove('is-animating');
                  gsap.set(element, { clearProps: 'opacity,visibility,transform' });
                },
              },
            );
            this.observer?.unobserve(element);
          }
        }
      },
      { threshold: 0.12, rootMargin: '0px 0px -40px 0px' },
    );
    this.observer.observe(element);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.tween?.kill();
  }
}
