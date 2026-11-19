import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, toApiError } from '../../core/api.service';
import { Address, AddressInput } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-account-addresses',
  imports: [ReactiveFormsModule],
  template: `
    <section class="account-page"><header class="page-head"><div><p class="eyebrow">Ready for next time</p><h2>Saved addresses</h2><p>Keep the details you use at checkout close at hand.</p></div><button type="button" class="btn btn--primary" (click)="addAddress()">Add address</button></header>
      @if(error()){<div class="notice--error" role="alert">{{error()}}</div>}
      @if(loading()){<p class="muted" role="status">Loading addresses…</p>}
      @else if(addresses().length){<div class="address-grid">@for(address of addresses();track address.id){<article class="address-card"><header><strong>{{address.label}}</strong><div>@if(address.isDefaultShipping){<span class="tag">Shipping</span>} @if(address.isDefaultBilling){<span class="tag">Billing</span>}</div></header><p><strong>{{address.fullName}}</strong></p><p>{{address.addressLine1}}</p>@if(address.addressLine2){<p>{{address.addressLine2}}</p>}<p>{{address.city}}, {{address.state}} {{address.postalCode}}</p><p>{{address.country}} · {{address.phone}}</p><footer><button type="button" (click)="editAddress(address)">Edit</button><button type="button" class="danger-link" (click)="deleteAddress(address)">Remove</button></footer></article>}</div>}
      @else if(!editing()){<div class="empty-state"><h3>No saved addresses</h3><p>Add one once and it will be ready for your next order.</p><button type="button" class="btn btn--ghost" (click)="addAddress()">Add an address</button></div>}
      @if(editing()){<section class="form-card"><div class="form-head"><h3>{{editingId()?'Edit address':'New address'}}</h3><button type="button" class="text-button" (click)="cancelEdit()">Cancel</button></div>
        <form [formGroup]="form" (ngSubmit)="save()">
          <div class="field-grid"><label>Address label<input class="input" formControlName="label" maxlength="50" /></label><label>Full name<input class="input" formControlName="fullName" autocomplete="name" maxlength="150" required /></label><label>Phone<input class="input" formControlName="phone" autocomplete="tel" maxlength="30" required /></label><label>Address line 1<input class="input" formControlName="addressLine1" autocomplete="address-line1" maxlength="250" required /></label><label>Address line 2 <small>Optional</small><input class="input" formControlName="addressLine2" autocomplete="address-line2" maxlength="250" /></label><label>City<input class="input" formControlName="city" autocomplete="address-level2" maxlength="100" required /></label><label>State<input class="input" formControlName="state" autocomplete="address-level1" maxlength="100" required /></label><label>Postal code<input class="input" formControlName="postalCode" autocomplete="postal-code" maxlength="20" required /></label><label>Country<input class="input" formControlName="country" autocomplete="country-name" maxlength="100" required /></label></div>
          <label class="check"><input type="checkbox" formControlName="isDefaultShipping" /> Default shipping address</label><label class="check"><input type="checkbox" formControlName="isDefaultBilling" /> Default billing address</label>
          <button type="submit" class="btn btn--primary" [disabled]="saving()">{{saving()?'Saving…':'Save address'}}</button>
        </form>
      </section>}
    </section>
  `,
  styles: `
    :host{display:block;color:#3d2a2f}.account-page{display:grid;gap:15px}.page-head{display:flex;justify-content:space-between;align-items:flex-end;gap:12px}.page-head h2{margin:7px 0;font:500 1.6rem Georgia,serif}.page-head p:not(.eyebrow){margin:0;color:#806e72;font-size:.84rem}.eyebrow{margin:0;color:#8e4a5b;font-size:.67rem;font-weight:700;letter-spacing:.16em;text-transform:uppercase}.address-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.address-card,.form-card{padding:18px;border:1px solid #eadfd9;border-radius:14px;background:#fffdfb}.address-card header,.address-card footer,.form-head{display:flex;align-items:center;justify-content:space-between;gap:8px}.address-card header>strong{font:500 1.1rem Georgia,serif}.address-card p{margin:5px 0;color:#705d61;font-size:.82rem}.address-card footer{justify-content:flex-start;margin-top:14px;padding-top:11px;border-top:1px solid #f0e8e3}.address-card button,.text-button{padding:0;border:0;background:none;color:#8e4a5b;font:inherit;font-size:.78rem;font-weight:700;cursor:pointer}.address-card .danger-link{color:#a33145}.tag{display:inline-block;margin-left:4px;padding:4px 7px;border-radius:999px;background:#f5eeeb;color:#8e4a5b;font-size:.65rem}.form-card h3{margin:0;font:500 1.2rem Georgia,serif}.form-head{margin-bottom:16px}.field-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.field-grid label{display:grid;gap:6px;font-size:.79rem;font-weight:600}.field-grid .input{width:100%;padding:10px 12px;border:1px solid #ded0ca;border-radius:9px;background:#fff}.field-grid small{color:#806e72;font-weight:400}.check{display:flex;align-items:center;gap:9px;margin-top:12px;color:#705d61;font-size:.82rem}.check input{accent-color:#8e4a5b}.form-card form>.btn{margin-top:16px}.empty-state{padding:35px 18px;border:1px dashed #d9c8c2;border-radius:14px;text-align:center}.empty-state p{color:#806e72}.notice--error{padding:12px;border-radius:10px;color:#7a2636;background:#fbe9ea}@media(max-width:620px){.page-head{align-items:flex-start;flex-direction:column}.address-grid,.field-grid{grid-template-columns:1fr}}
  `,
})
export class AccountAddressesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly addresses = signal<Address[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly editing = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly form = this.fb.nonNullable.group({
    label: ['Home', [Validators.required, Validators.maxLength(50)]],
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    phone: ['', [Validators.required, Validators.maxLength(30)]],
    addressLine1: ['', [Validators.required, Validators.maxLength(250)]],
    addressLine2: ['', [Validators.maxLength(250)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    state: ['Karnataka', [Validators.required, Validators.maxLength(100)]],
    postalCode: ['', [Validators.required, Validators.maxLength(20)]],
    country: ['India', [Validators.required, Validators.maxLength(100)]],
    isDefaultShipping: [false],
    isDefaultBilling: [false],
  });

  ngOnInit(): void { void this.load(); }

  protected addAddress(): void {
    this.editingId.set(null); this.form.reset({ label: 'Home', fullName: '', phone: '', addressLine1: '', addressLine2: '', city: '', state: 'Karnataka', postalCode: '', country: 'India', isDefaultShipping: !this.addresses().length, isDefaultBilling: !this.addresses().length }); this.editing.set(true);
  }
  protected editAddress(address: Address): void {
    this.editingId.set(address.id); this.form.reset({ ...address, addressLine2: address.addressLine2 ?? '' }); this.editing.set(true);
  }
  protected cancelEdit(): void { this.editing.set(false); this.editingId.set(null); }
  protected async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true); this.error.set(null);
    try {
      const payload: AddressInput = this.form.getRawValue();
      if (this.editingId()) await this.api.put(`account/addresses/${this.editingId()}`, payload);
      else await this.api.post('account/addresses', payload);
      this.cancelEdit(); await this.load(); this.toast.success('Your address has been saved.', 'Address saved');
    } catch (error) { this.error.set(toApiError(error).message); }
    finally { this.saving.set(false); }
  }
  protected async deleteAddress(address: Address): Promise<void> {
    if (!window.confirm(`Remove the ${address.label.toLowerCase()} address from your account?`)) return;
    try { await this.api.delete(`account/addresses/${address.id}`); await this.load(); this.toast.success('The address was removed.', 'Address removed'); }
    catch (error) { this.error.set(toApiError(error).message); }
  }
  private async load(): Promise<void> {
    this.loading.set(true); this.error.set(null);
    try { this.addresses.set(await this.api.get<Address[]>('account/addresses')); }
    catch (error) { this.error.set(toApiError(error).message); }
    finally { this.loading.set(false); }
  }
}
