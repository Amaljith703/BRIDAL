import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `<main class="not-found-page"><div class="not-found-card"><p class="eyebrow">A little detour</p><p class="not-found-code" aria-hidden="true">404</p><h1>That page has wandered off.</h1><p>We couldn't find that address. Head back to the studio and we'll help you find the right look.</p><a class="button button--primary" routerLink="/">Return to the studio</a></div></main>`,
  styles: `:host{display:block}.not-found-page{display:grid;min-height:65vh;place-items:center;padding:80px 24px;background:radial-gradient(circle at 70% 15%,#f4e6e1,transparent 48%),#faf6f1}.not-found-card{max-width:620px;text-align:center}.not-found-code{margin:20px 0 0;color:#c9a26b;font:500 clamp(5rem,18vw,12rem)/.8 Georgia,serif;letter-spacing:-.08em}.not-found-card h1{margin:24px 0 16px;font-size:clamp(2.5rem,6vw,4.5rem)}.not-found-card>p:not(.eyebrow):not(.not-found-code){max-width:480px;margin:0 auto 26px;color:#705d61}.not-found-card a{display:inline-flex;align-items:center;justify-content:center;padding:12px 22px;border-radius:999px;background:#8e4a5b;color:#fff;text-decoration:none}`,
})
export class NotFoundComponent {}
