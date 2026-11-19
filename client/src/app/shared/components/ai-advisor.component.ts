import { Component, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, toApiError } from '../../core/api.service';
import { AiChatResponse, AiChatMessage, Product } from '../../core/models';
import { SiteService } from '../../core/site.service';
import { IconComponent } from '../icon.component';
import { InrPipe } from '../pipes';

interface ChatBubble extends AiChatMessage {
  products?: Product[];
}

@Component({
  selector: 'app-ai-advisor',
  imports: [RouterLink, IconComponent, InrPipe],
  template: `
    <div class="advisor" [class.is-open]="open()">
      @if (open()) {
        <section class="advisor__panel" role="dialog" aria-label="Beauty advisor">
          <header class="advisor__head">
            <span class="advisor__orb" aria-hidden="true"><app-icon name="bot" [size]="19" /></span>
            <div class="advisor__ident">
              <strong>Studio beauty advisor</strong>
              <span><i></i> Local AI · private to this studio</span>
            </div>
            <button type="button" class="advisor__close" (click)="open.set(false)" aria-label="Close advisor">
              <app-icon name="close" [size]="17" />
            </button>
          </header>

          <div class="advisor__thread" #thread>
            @for (message of messages(); track $index) {
              <article class="bubble" [class.bubble--user]="message.role === 'user'">
                @if (message.role === 'assistant') {
                  <span class="bubble__mark" aria-hidden="true">MA</span>
                }
                <div class="bubble__body">
                  <p>{{ message.content }}</p>
                  @if (message.products?.length) {
                    <div class="bubble__products">
                      @for (product of message.products; track product.id) {
                        <a [routerLink]="['/products', product.slug]" (click)="open.set(false)">
                          <img [src]="product.imageUrl || ''" [alt]="product.name" loading="lazy" />
                          <span>
                            <strong>{{ product.name }}</strong>
                            <small>{{ product.price | inr }}</small>
                          </span>
                        </a>
                      }
                    </div>
                  }
                </div>
              </article>
            }

            @if (thinking()) {
              <article class="bubble">
                <span class="bubble__mark" aria-hidden="true">MA</span>
                <div class="bubble__body bubble__body--typing" aria-label="Advisor is thinking">
                  <i></i><i></i><i></i>
                </div>
              </article>
            }
          </div>

          @if (messages().length <= 1) {
            <div class="advisor__prompts">
              @for (prompt of prompts; track prompt) {
                <button type="button" (click)="send(prompt)">{{ prompt }}</button>
              }
            </div>
          }

          <form class="advisor__compose" (submit)="submit($event)">
            <input
              type="text"
              name="message"
              [value]="draft()"
              (input)="draft.set($any($event.target).value)"
              placeholder="Ask about skin, shade or occasion…"
              maxlength="600"
              aria-label="Message the beauty advisor"
            />
            <button type="submit" [disabled]="busy() || !draft().trim()" aria-label="Send message">
              <app-icon name="send" [size]="17" />
            </button>
          </form>
          <p class="advisor__foot">Guidance only — patch-test and consult in studio for allergies.</p>
        </section>
      }

      @if (!open()) {
        <button type="button" class="advisor__fab" (click)="open.set(true)" aria-label="Open the AI beauty advisor">
          <app-icon name="sparkles" [size]="20" />
          <span class="advisor__fab-text">Ask the<br />beauty advisor</span>
        </button>
      }
    </div>
  `,
  styles: `
    .advisor {
      position: fixed;
      right: 22px;
      bottom: 22px;
      z-index: 1500;
      font-family: var(--font-body);
    }
    .advisor__fab {
      display: flex;
      align-items: center;
      gap: 11px;
      padding: 12px 18px 12px 14px;
      border-radius: 999px;
      background:
        radial-gradient(120% 120% at 10% 0%, rgba(233, 200, 193, 0.28), transparent 55%),
        #332226;
      color: #faf6f1;
      border: 1px solid rgba(233, 200, 193, 0.28);
      box-shadow: 0 22px 48px -20px rgba(20, 10, 14, 0.7);
      text-align: left;
      transition: transform 0.3s var(--ease), box-shadow 0.3s var(--ease);
    }
    .advisor__fab:hover {
      transform: translateY(-3px);
      box-shadow: 0 30px 60px -22px rgba(20, 10, 14, 0.75);
    }
    .advisor__fab app-icon {
      color: #e9c8c1;
    }
    .advisor__fab-text {
      font-size: 0.66rem;
      font-weight: 600;
      line-height: 1.25;
      letter-spacing: 0.06em;
      text-transform: uppercase;
    }
    .advisor__panel {
      width: min(396px, calc(100vw - 28px));
      max-height: min(660px, calc(100vh - 44px));
      display: flex;
      flex-direction: column;
      border-radius: 22px;
      overflow: hidden;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.13);
      box-shadow: 0 44px 90px -30px rgba(28, 14, 18, 0.55);
      animation: advisorRise 0.35s var(--ease);
    }
    .advisor__head {
      display: flex;
      align-items: center;
      gap: 11px;
      padding: 15px 16px;
      background: #332226;
      color: #faf6f1;
    }
    .advisor__orb {
      display: grid;
      place-items: center;
      width: 38px;
      height: 38px;
      border-radius: 50%;
      background: linear-gradient(140deg, #c9a26b, #8e4a5b);
      color: #fff;
      flex-shrink: 0;
    }
    .advisor__ident {
      display: grid;
      gap: 2px;
      flex: 1;
      min-width: 0;
    }
    .advisor__ident strong {
      font-family: var(--font-display);
      font-size: 1.18rem;
      font-weight: 500;
      letter-spacing: 0.01em;
    }
    .advisor__ident span {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 0.62rem;
      color: rgba(250, 246, 241, 0.55);
    }
    .advisor__ident i {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #7bc39a;
      box-shadow: 0 0 0 3px rgba(123, 195, 154, 0.16);
    }
    .advisor__close {
      display: grid;
      place-items: center;
      width: 30px;
      height: 30px;
      border-radius: 50%;
      color: rgba(250, 246, 241, 0.7);
    }
    .advisor__close:hover {
      background: rgba(250, 246, 241, 0.1);
      color: #fff;
    }
    .advisor__thread {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 12px;
      background:
        radial-gradient(520px 220px at 100% 0%, rgba(233, 200, 193, 0.16), transparent 65%),
        #faf6f1;
      min-height: 240px;
    }
    .bubble {
      display: flex;
      gap: 8px;
      align-items: flex-start;
      max-width: 94%;
    }
    .bubble--user {
      align-self: flex-end;
      flex-direction: row-reverse;
    }
    .bubble__mark {
      display: grid;
      place-items: center;
      width: 26px;
      height: 26px;
      border-radius: 50%;
      background: #8e4a5b;
      color: #fff;
      font-family: var(--font-display);
      font-size: 0.56rem;
      letter-spacing: 0.04em;
      flex-shrink: 0;
      margin-top: 2px;
    }
    .bubble__body {
      padding: 11px 14px;
      border-radius: 4px 16px 16px 16px;
      background: #fffdfb;
      border: 1px solid rgba(61, 42, 47, 0.09);
      font-size: 0.82rem;
      line-height: 1.6;
      color: #4d3a40;
      box-shadow: 0 10px 24px -20px rgba(49, 26, 32, 0.5);
    }
    .bubble--user .bubble__body {
      background: #8e4a5b;
      border-color: #8e4a5b;
      color: #fff;
      border-radius: 16px 4px 16px 16px;
    }
    .bubble__body p {
      margin: 0;
      white-space: pre-line;
    }
    .bubble__body--typing {
      display: flex;
      gap: 4px;
      padding: 15px 16px;
    }
    .bubble__body--typing i {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #b99aa2;
      animation: typing 1s ease-in-out infinite;
    }
    .bubble__body--typing i:nth-child(2) {
      animation-delay: 0.15s;
    }
    .bubble__body--typing i:nth-child(3) {
      animation-delay: 0.3s;
    }
    .bubble__products {
      display: grid;
      gap: 6px;
      margin-top: 10px;
    }
    .bubble__products a {
      display: flex;
      align-items: center;
      gap: 9px;
      padding: 6px;
      border-radius: 10px;
      background: #faf6f1;
      border: 1px solid rgba(61, 42, 47, 0.08);
    }
    .bubble__products a:hover {
      border-color: rgba(142, 74, 91, 0.35);
    }
    .bubble__products img {
      width: 38px;
      height: 44px;
      object-fit: cover;
      border-radius: 7px;
      background: #f3ebe2;
      flex-shrink: 0;
    }
    .bubble__products span {
      display: grid;
      gap: 1px;
      min-width: 0;
    }
    .bubble__products strong {
      font-size: 0.72rem;
      font-weight: 600;
      color: #332226;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .bubble__products small {
      font-size: 0.64rem;
      color: #8e4a5b;
    }
    .advisor__prompts {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
      padding: 0 16px 10px;
      background: #faf6f1;
    }
    .advisor__prompts button {
      padding: 6px 11px;
      border: 1px solid rgba(61, 42, 47, 0.14);
      border-radius: 999px;
      font-size: 0.68rem;
      color: #6f5a60;
      background: #fffdfb;
    }
    .advisor__prompts button:hover {
      border-color: #8e4a5b;
      color: #8e4a5b;
    }
    .advisor__compose {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 14px;
      border-top: 1px solid rgba(61, 42, 47, 0.09);
      background: #fffdfb;
    }
    .advisor__compose input {
      flex: 1;
      min-width: 0;
      border: 1px solid rgba(61, 42, 47, 0.13);
      border-radius: 999px;
      padding: 11px 15px;
      font-size: 0.82rem;
      background: #faf6f1;
      outline: none;
    }
    .advisor__compose input:focus {
      border-color: #8e4a5b;
      background: #fff;
    }
    .advisor__compose button {
      display: grid;
      place-items: center;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #8e4a5b;
      color: #fff;
      flex-shrink: 0;
      transition: background 0.2s ease;
    }
    .advisor__compose button:hover:not(:disabled) {
      background: #5e2e3c;
    }
    .advisor__compose button:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }
    .advisor__foot {
      margin: 0;
      padding: 0 16px 12px;
      font-size: 0.6rem;
      color: #a28d93;
      text-align: center;
      background: #fffdfb;
    }
    @keyframes typing {
      0%,
      100% {
        transform: translateY(0);
        opacity: 0.4;
      }
      50% {
        transform: translateY(-4px);
        opacity: 1;
      }
    }
    @keyframes advisorRise {
      from {
        opacity: 0;
        transform: translateY(16px) scale(0.98);
      }
    }
    @media (max-width: 520px) {
      .advisor {
        right: 12px;
        bottom: 78px;
      }
      .advisor__fab {
        padding: 11px 14px;
      }
      .advisor__fab-text {
        display: none;
      }
      .advisor__panel {
        max-height: calc(100vh - 96px);
      }
    }
  `,
})
export class AiAdvisorComponent {
  private readonly api = inject(ApiService);
  private readonly site = inject(SiteService);
  private readonly thread = viewChild<ElementRef<HTMLElement>>('thread');

  protected readonly open = signal(false);
  protected readonly thinking = signal(false);
  protected readonly draft = signal('');
  protected readonly busy = computed(() => this.thinking());
  protected readonly messages = signal<ChatBubble[]>([
    {
      role: 'assistant',
      content:
        'Hello, lovely. I’m the studio’s beauty advisor. Tell me about your skin, the occasion, or the look you have in mind and I’ll suggest a routine from our edit.',
    },
  ]);

  protected readonly prompts = [
    'Bridal glow for warm undertones',
    'A 5-minute everyday face',
    'What suits oily skin?',
  ];

  protected submit(event: Event): void {
    event.preventDefault();
    void this.send(this.draft().trim());
  }

  protected async send(text: string): Promise<void> {
    const message = text.trim();
    if (!message || this.thinking()) return;
    this.draft.set('');
    this.messages.update((list) => [...list, { role: 'user', content: message }]);
    this.thinking.set(true);
    this.scrollSoon();

    try {
      const history = this.messages()
        .filter((entry) => entry.content)
        .slice(-10)
        .map((entry) => ({ role: entry.role, content: entry.content }));
      const response = await this.api.post<AiChatResponse>('ai/beauty-advisor', { messages: history });
      const recommended = this.pickProducts(response.recommendedProductIds ?? []);
      this.messages.update((list) => [
        ...list,
        {
          role: 'assistant',
          content: response.content,
          products: recommended,
        },
      ]);
    } catch (error) {
      this.messages.update((list) => [
        ...list,
        {
          role: 'assistant',
          content:
            toApiError(error).message ||
            'I’m offline for a moment. You can still browse the studio edit or book a consultation and we’ll talk it through in person.',
        },
      ]);
    } finally {
      this.thinking.set(false);
      this.scrollSoon();
    }
  }

  private pickProducts(ids: string[]): Product[] {
    const catalogue = this.site.featuredProducts();
    const all = this.site.data().featuredProducts;
    const pool = [...all, ...catalogue];
    return ids
      .map((id) => pool.find((product) => product.id === id))
      .filter((product): product is Product => !!product)
      .slice(0, 3);
  }

  private scrollSoon(): void {
    requestAnimationFrame(() => {
      const el = this.thread()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }
}
