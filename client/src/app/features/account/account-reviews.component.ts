import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, toApiError } from '../../core/api.service';
import { Order, OrderSummary, Paged, ProductDetail, Review } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { DateLabelPipe } from '../../shared/pipes';

@Component({
  selector: 'app-account-reviews',
  imports: [ReactiveFormsModule, DateLabelPipe],
  template: `
    <section class="account-page"><header class="page-head"><div><p class="eyebrow">Your point of view</p><h2>Reviews</h2><p>Share a note about a product you have purchased.</p></div></header>
      @if(error()){<div class="notice--error" role="alert">{{error()}}</div>}
      @if(eligibleProducts().length){<section class="panel"><h3>Write a review</h3><form [formGroup]="form" (ngSubmit)="submit()">
        <label>Purchased product<select class="input" formControlName="productId" required><option value="">Choose a product</option>@for(product of eligibleProducts();track product.id){<option [value]="product.id">{{product.name}}</option>}</select></label>
        <label>Rating<select class="input" formControlName="rating"><option [ngValue]="5">5 · Loved it</option><option [ngValue]="4">4 · Really liked it</option><option [ngValue]="3">3 · It was good</option><option [ngValue]="2">2 · Could be better</option><option [ngValue]="1">1 · Not for me</option></select></label>
        <label>Title<input class="input" formControlName="title" maxlength="150" required /></label>
        <label>Your review<textarea class="input" formControlName="body" rows="4" minlength="10" maxlength="3000" required placeholder="What did you like about it? What should another customer know?"></textarea></label>
        <button type="submit" class="btn btn--primary" [disabled]="saving()">{{saving()?'Sending…':'Submit review'}}</button>
      </form></section>}
      @if(loading()){<p class="muted" role="status">Loading your reviews…</p>}
      @else if(reviews().length){<div class="review-list">@for(review of reviews();track review.id){<article class="review-card"><header><span class="stars" [attr.aria-label]="review.rating + ' out of 5 stars'">{{'★'.repeat(review.rating)}}{{'☆'.repeat(5-review.rating)}}</span><span class="status-pill">{{review.status}}</span></header><h3>{{review.title}}</h3><p class="review-product">{{review.productName}} · {{review.createdAtUtc | dateLabel}}</p><p>{{review.body}}</p>@if(review.isVerifiedPurchase){<span class="verified">Verified purchase</span>}@if(review.adminResponse){<blockquote><strong>A note from the studio</strong><p>{{review.adminResponse}}</p></blockquote>}<button type="button" class="remove-link" (click)="remove(review)">Remove review</button></article>}</div>}
      @else if(!eligibleProducts().length){<div class="empty-state"><h3>No reviews to show yet</h3><p>After a paid product order, you can leave a verified review here.</p></div>}
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:14px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.panel,.review-card{padding:18px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.panel h3,.review-card h3{margin:0 0 12px;font:500 1.14rem Georgia,serif}.panel form{display:grid;gap:12px}.panel label{display:grid;gap:6px;font-size:.8rem;font-weight:600}.input{width:100%;padding:10px 12px;border:1px solid #ded0ca;border-radius:9px;background:#fff;font:inherit}.review-list{display:grid;gap:10px}.review-card header{display:flex;align-items:center;justify-content:space-between}.stars{color:#c3924c;letter-spacing:.08em}.status-pill{padding:5px 9px;border-radius:999px;background:#f5eeeb;font-size:.7rem}.review-card h3{margin:13px 0 4px}.review-card p{color:#705d61;font-size:.84rem;line-height:1.6}.review-product{margin:0;color:#806e72!important;font-size:.75rem!important}.verified{color:#315b43;font-size:.73rem;font-weight:700}.review-card blockquote{margin:14px 0;padding:12px;border-left:3px solid #c9a26b;background:#f8f1eb}.review-card blockquote p{margin:5px 0 0}.remove-link{margin-top:12px;padding:0;border:0;background:none;color:#a33145;font-size:.76rem;font-weight:700;cursor:pointer}.empty-state{padding:36px 18px;border:1px dashed #d9c8c2;border-radius:14px;text-align:center}.empty-state p,.muted{color:#806e72}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}
  `,
})
export class AccountReviewsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly reviews = signal<Review[]>([]);
  protected readonly eligibleProducts = signal<Array<{ id: string; name: string }>>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = this.fb.nonNullable.group({ productId: ['', Validators.required], rating: [5], title: ['', [Validators.required, Validators.maxLength(150)]], body: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(3000)]] });
  protected readonly reviewedIds = computed(() => new Set(this.reviews().map((review) => review.productId)));

  ngOnInit(): void { void this.load(); }

  protected async submit(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); this.error.set(null);
    try {
      const value = this.form.getRawValue();
      const review = await this.api.post<Review>(`account/products/${value.productId}/reviews`, { rating: value.rating, title: value.title.trim(), body: value.body.trim() });
      this.reviews.update((reviews) => [review, ...reviews]);
      this.eligibleProducts.update((products) => products.filter((product) => product.id !== value.productId));
      this.form.reset({ productId: '', rating: 5, title: '', body: '' });
      this.toast.success('Thank you for sharing your experience. Your review will appear after moderation.', 'Review received');
    } catch (error) { this.error.set(toApiError(error).message); }
    finally { this.saving.set(false); }
  }

  protected async remove(review: Review): Promise<void> {
    if (!window.confirm('Remove this review from your account?')) return;
    try { await this.api.delete(`account/reviews/${review.id}`); this.reviews.update((reviews) => reviews.filter((item) => item.id !== review.id)); }
    catch (error) { this.error.set(toApiError(error).message); }
  }

  private async load(): Promise<void> {
    this.loading.set(true); this.error.set(null);
    try {
      const [reviews, orders] = await Promise.all([
        this.api.get<Review[]>('account/reviews'),
        this.api.get<Paged<OrderSummary>>('account/orders', { page: 1, pageSize: 50 }),
      ]);
      this.reviews.set(reviews);
      const paid = orders.items.filter((order) => order.paymentStatus === 'Captured');
      const detailResults = await Promise.allSettled(paid.map((order) => this.api.get<Order>(`account/orders/${order.id}`)));
      const candidates = new Map<string, string>();
      for (const result of detailResults) {
        if (result.status !== 'fulfilled') continue;
        for (const line of result.value.items) if (line.itemType === 'Product') candidates.set(line.itemId, line.name);
      }
      this.eligibleProducts.set([...candidates].filter(([id]) => !this.reviewedIds().has(id)).map(([id, name]) => ({ id, name })));
    } catch (error) { this.error.set(toApiError(error).message); }
    finally { this.loading.set(false); }
  }
}
