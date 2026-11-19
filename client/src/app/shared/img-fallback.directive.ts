import { Directive, ElementRef, HostListener, Input, OnChanges, OnInit, inject } from '@angular/core';

const PLACEHOLDER = `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(`
<svg xmlns="http://www.w3.org/2000/svg" width="900" height="1100" viewBox="0 0 900 1100">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#E9C8C1"/><stop offset="0.55" stop-color="#D9B8A6"/><stop offset="1" stop-color="#8E4A5B"/>
    </linearGradient>
  </defs>
  <rect width="900" height="1100" fill="url(#g)"/>
  <circle cx="450" cy="470" r="160" fill="none" stroke="#FAF6F1" stroke-opacity=".55" stroke-width="2"/>
  <path d="M450 330c74 67 108 123 108 168 0 60-48 108-108 108s-108-48-108-108c0-45 34-101 108-168Z" fill="#FAF6F1" fill-opacity=".35"/>
  <text x="450" y="710" text-anchor="middle" fill="#FAF6F1" font-family="Georgia,serif" font-size="42" letter-spacing="6">MAKEUP ART</text>
</svg>`)}`;

@Directive({ selector: 'img[appImgFallback]' })
export class ImgFallbackDirective implements OnInit, OnChanges {
  private readonly el = inject(ElementRef<HTMLImageElement>);
  @Input() appImgFallback: string | null | undefined;

  private failed = false;

  ngOnInit(): void {
    if (!this.el.nativeElement.getAttribute('loading')) this.el.nativeElement.loading = 'lazy';
    this.el.nativeElement.decoding = 'async';
  }

  ngOnChanges(): void {
    this.failed = false;
    const image = this.el.nativeElement;
    if (!this.appImgFallback && !image.getAttribute('src')) image.src = PLACEHOLDER;
  }

  @HostListener('error')
  onError(): void {
    if (this.failed) return;
    this.failed = true;
    this.el.nativeElement.src = PLACEHOLDER;
    this.el.nativeElement.classList.add('image-fallback');
  }
}
