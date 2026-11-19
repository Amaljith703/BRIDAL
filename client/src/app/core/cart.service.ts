import { Injectable, computed, inject, signal } from '@angular/core';
import { CartLine, Combo, OrderItemType, Product, ProductVariant, Service, ServicePackage } from './models';
import { ToastService } from './toast.service';

const CART_KEY = 'ma_cart_v1';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly toast = inject(ToastService);
  readonly lines = signal<CartLine[]>(this.read());
  readonly count = computed(() => this.lines().reduce((total, line) => total + line.quantity, 0));
  readonly subtotal = computed(() => Math.round(this.lines().reduce((total, line) => total + line.unitPrice * line.quantity, 0) * 100) / 100);
  readonly isEmpty = computed(() => this.lines().length === 0);
  readonly hasPhysicalItems = computed(() => this.lines().some((line) => line.itemType === 'Product' || line.itemType === 'Combo'));

  addProduct(product: Product, variant?: ProductVariant | null, quantity = 1): void {
    const unitPrice = variant?.priceOverride ?? product.price;
    const stock = variant?.stockQuantity ?? product.stockQuantity;
    this.add({
      key: `Product:${product.id}:${variant?.name ?? ''}`,
      itemType: 'Product',
      itemId: product.id,
      name: product.name,
      slug: product.slug,
      imageUrl: variant?.imageUrl || product.imageUrl,
      unitPrice,
      quantity,
      variantName: variant?.name ?? null,
      maxQuantity: Math.max(1, Math.min(20, stock || 1)),
    });
  }

  addService(service: Service): void {
    this.add({
      key: `Service:${service.id}:`,
      itemType: 'Service',
      itemId: service.id,
      name: service.name,
      slug: service.slug,
      imageUrl: service.imageUrl,
      unitPrice: service.price,
      quantity: 1,
      variantName: null,
      maxQuantity: 20,
    });
  }

  addPackage(packageItem: ServicePackage): void {
    this.add({
      key: `Package:${packageItem.id}:`,
      itemType: 'Package',
      itemId: packageItem.id,
      name: packageItem.name,
      slug: packageItem.slug,
      imageUrl: packageItem.imageUrl,
      unitPrice: packageItem.price,
      quantity: 1,
      variantName: null,
      maxQuantity: 20,
    });
  }

  addCombo(combo: Combo): void {
    this.add({
      key: `Combo:${combo.id}:`,
      itemType: 'Combo',
      itemId: combo.id,
      name: combo.name,
      slug: combo.slug,
      imageUrl: combo.imageUrl,
      unitPrice: combo.price,
      quantity: 1,
      variantName: null,
      maxQuantity: 20,
    });
  }

  updateQuantity(key: string, quantity: number): void {
    this.lines.update((lines) =>
      lines
        .map((line) =>
          line.key === key
            ? { ...line, quantity: Math.max(1, Math.min(line.maxQuantity, Math.floor(quantity) || 1)) }
            : line,
        )
        .filter((line) => line.quantity > 0),
    );
    this.persist();
  }

  increment(key: string): void {
    const line = this.lines().find((item) => item.key === key);
    if (line) this.updateQuantity(key, line.quantity + 1);
  }

  decrement(key: string): void {
    const line = this.lines().find((item) => item.key === key);
    if (!line) return;
    if (line.quantity <= 1) this.remove(key);
    else this.updateQuantity(key, line.quantity - 1);
  }

  remove(key: string): void {
    this.lines.update((lines) => lines.filter((line) => line.key !== key));
    this.persist();
    this.toast.info('Removed from your bag.');
  }

  clear(): void {
    this.lines.set([]);
    this.persist();
  }

  itemsForApi(): Array<{ itemType: OrderItemType; itemId: string; quantity: number; variantName?: string | null }> {
    return this.lines().map((line) => ({
      itemType: line.itemType,
      itemId: line.itemId,
      quantity: line.quantity,
      variantName: line.variantName,
    }));
  }

  quantityFor(itemType: OrderItemType, id: string): number {
    return this.lines()
      .filter((line) => line.itemType === itemType && line.itemId === id)
      .reduce((total, line) => total + line.quantity, 0);
  }

  private add(line: CartLine): void {
    this.lines.update((lines) => {
      const existing = lines.find((item) => item.key === line.key);
      if (existing) {
        return lines.map((item) =>
          item.key === line.key
            ? { ...item, quantity: Math.min(item.maxQuantity, item.quantity + line.quantity) }
            : item,
        );
      }
      return [...lines, line];
    });
    this.persist();
    this.toast.success(`${line.name} is in your bag.`, 'Added with care');
  }

  private persist(): void {
    try {
      localStorage.setItem(CART_KEY, JSON.stringify(this.lines()));
    } catch {
      // Cart remains in memory when storage is unavailable.
    }
  }

  private read(): CartLine[] {
    try {
      const raw = localStorage.getItem(CART_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as CartLine[];
      if (!Array.isArray(parsed)) return [];
      return parsed
        .filter((line) => line && line.key && line.itemId && line.quantity > 0)
        .map((line) => ({ ...line, quantity: Math.max(1, Math.min(line.maxQuantity || 20, line.quantity)) }));
    } catch {
      return [];
    }
  }
}
