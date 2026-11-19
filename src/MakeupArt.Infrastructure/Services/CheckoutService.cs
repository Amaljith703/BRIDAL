using System.Security.Cryptography;
using System.Text.Json;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Application.Pricing;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MakeupArt.Infrastructure.Services;

public sealed record ResolvedCartLine(
    OrderItemType ItemType,
    Guid ItemId,
    string Name,
    string? Sku,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal TaxRatePercent,
    string? HsnCode,
    string? VariantName,
    bool IsPhysical,
    Guid? VariantId = null);

public sealed class QuoteOutcome
{
    public IReadOnlyList<ResolvedCartLine> Lines { get; init; } = [];
    public PricingResult Pricing { get; init; } = new();
    public Coupon? Coupon { get; init; }
    public string? CouponMessage { get; init; }
    public bool CouponApplied { get; init; }
    public bool HasPhysicalItems { get; init; }
    public ShippingMethod? ShippingMethod { get; init; }
    public string ShippingMethodName { get; init; } = "Studio appointment";
}

public interface ICheckoutService
{
    Task<QuoteOutcome> QuoteAsync(IReadOnlyList<CartItemRequest> items, string? couponCode, Guid? shippingMethodId,
        string? placeOfSupply, Guid? userId, CancellationToken cancellationToken = default);
    Task<Order> CreateOrderAsync(Guid userId, CheckoutRequest request, QuoteOutcome quote, CancellationToken cancellationToken = default);
    Task<Order> CompletePaymentAsync(Guid orderId, string providerOrderId, string providerPaymentId, string? method, CancellationToken cancellationToken = default);
    Task<int> DecrementStockAsync(Order order, CancellationToken cancellationToken = default);
}

public class CheckoutService(AppDbContext db, ILogger<CheckoutService> logger) : ICheckoutService
{
    public async Task<QuoteOutcome> QuoteAsync(
        IReadOnlyList<CartItemRequest> items,
        string? couponCode,
        Guid? shippingMethodId,
        string? placeOfSupply,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) throw ApiException.Validation("Your bag is empty.");
        if (items.Count > 30) throw ApiException.Validation("Too many different items in one order.");

        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                       ?? throw ApiException.NotFound("Site settings");
        var lines = new List<ResolvedCartLine>();

        foreach (var item in items)
        {
            var quantity = Math.Clamp(item.Quantity, 1, 20);
            switch (item.ItemType)
            {
                case OrderItemType.Product:
                {
                    var product = await db.Products.AsNoTracking()
                        .Include(x => x.TaxRate)
                        .Include(x => x.Variants)
                        .FirstOrDefaultAsync(x => x.Id == item.ItemId && x.IsActive, cancellationToken)
                        ?? throw ApiException.NotFound("Product");
                    ProductVariant? variant = null;
                    if (!string.IsNullOrWhiteSpace(item.VariantName))
                    {
                        variant = product.Variants.FirstOrDefault(v => v.IsActive &&
                            (string.Equals(v.Name, item.VariantName, StringComparison.OrdinalIgnoreCase) || string.Equals(v.Sku, item.VariantName, StringComparison.OrdinalIgnoreCase)));
                        if (variant is null) throw ApiException.Validation($"The variant '{item.VariantName}' is no longer available for {product.Name}.");
                    }
                    var available = variant?.StockQuantity ?? product.StockQuantity;
                    if (available < quantity)
                        throw ApiException.Validation($"Only {Math.Max(0, available)} left of {product.Name}{(variant is null ? "" : $" ({variant.Name})")}.");
                    var price = variant?.PriceOverride ?? product.Price;
                    lines.Add(new ResolvedCartLine(OrderItemType.Product, product.Id, product.Name, variant?.Sku ?? product.Sku,
                        variant?.ImageUrl ?? product.ImageUrl, price, quantity, settings.GstEnabled ? product.TaxRate?.RatePercent ?? 18m : 0m,
                        product.HsnCode, variant?.Name, true, variant?.Id));
                    break;
                }
                case OrderItemType.Service:
                {
                    var service = await db.Services.AsNoTracking().Include(x => x.TaxRate)
                        .FirstOrDefaultAsync(x => x.Id == item.ItemId && x.IsActive, cancellationToken)
                        ?? throw ApiException.NotFound("Service");
                    lines.Add(new ResolvedCartLine(OrderItemType.Service, service.Id, service.Name, null, service.ImageUrl,
                        service.Price, quantity, settings.GstEnabled ? service.TaxRate?.RatePercent ?? 18m : 0m,
                        service.HsnCode, null, false));
                    break;
                }
                case OrderItemType.Package:
                {
                    var package = await db.ServicePackages.AsNoTracking().Include(x => x.TaxRate)
                        .FirstOrDefaultAsync(x => x.Id == item.ItemId && x.IsActive, cancellationToken)
                        ?? throw ApiException.NotFound("Package");
                    lines.Add(new ResolvedCartLine(OrderItemType.Package, package.Id, package.Name, null, package.ImageUrl,
                        package.Price, quantity, settings.GstEnabled ? package.TaxRate?.RatePercent ?? 18m : 0m,
                        package.HsnCode, null, false));
                    break;
                }
                case OrderItemType.Combo:
                {
                    var combo = await db.ProductCombos.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == item.ItemId && x.IsActive, cancellationToken)
                        ?? throw ApiException.NotFound("Combo");
                    lines.Add(new ResolvedCartLine(OrderItemType.Combo, combo.Id, combo.Name, null, combo.ImageUrl,
                        combo.Price, quantity, settings.GstEnabled ? 18m : 0m, "3304", null, true));
                    break;
                }
                default:
                    throw ApiException.Validation("Unknown cart item type.");
            }
        }

        // Merge duplicate lines of the same product/variant.
        var merged = lines
            .GroupBy(x => new { x.ItemType, x.ItemId, x.VariantName })
            .Select(g =>
            {
                var first = g.First();
                var quantity = Math.Min(20, g.Sum(x => x.Quantity));
                return first with { Quantity = quantity };
            })
            .ToList();

        var hasPhysical = merged.Any(x => x.IsPhysical);
        ShippingMethod? shipping = null;
        if (hasPhysical)
        {
            shipping = shippingMethodId is not null
                ? await db.ShippingMethods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == shippingMethodId && x.IsActive, cancellationToken)
                : await db.ShippingMethods.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.IsDefault).ThenBy(x => x.SortOrder).FirstOrDefaultAsync(cancellationToken);
        }

        var productSubtotal = merged.Where(x => x.IsPhysical).Sum(x => x.UnitPrice * x.Quantity);
        var shippingAmount = shipping is null ? 0m
            : shipping.FreeAboveAmount is not null && productSubtotal >= shipping.FreeAboveAmount ? 0m
            : shipping.Price;

        Coupon? coupon = null;
        string? couponMessage = null;
        var couponApplied = false;
        var preDiscountSubtotal = merged.Sum(x => x.UnitPrice * x.Quantity);
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var normalized = couponCode.Trim().ToUpperInvariant();
            var now = DateTime.UtcNow;
            coupon = await db.Coupons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == normalized && x.IsActive, cancellationToken);
            if (coupon is null)
            {
                couponMessage = "That coupon code is not recognised.";
            }
            else if (coupon.StartsAtUtc > now || coupon.EndsAtUtc < now)
            {
                couponMessage = "That coupon has expired.";
            }
            else if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
            {
                couponMessage = "That coupon has reached its limit.";
            }
            else if (preDiscountSubtotal < coupon.MinimumOrderAmount)
            {
                couponMessage = $"Spend at least {Money.Format(coupon.MinimumOrderAmount, settings.Currency)} to use this code.";
            }
            else if (userId is not null)
            {
                var used = await db.CouponRedemptions.AsNoTracking()
                    .CountAsync(x => x.CouponId == coupon.Id && x.UserId == userId, cancellationToken);
                if (coupon.PerUserLimit > 0 && used >= coupon.PerUserLimit)
                    couponMessage = "You have already used this coupon.";
            }
            couponApplied = couponMessage is null;
        }

        var couponInput = coupon is not null && couponApplied
            ? new CouponInput(coupon.Code, coupon.DiscountType, coupon.DiscountValue, coupon.MinimumOrderAmount,
                coupon.MaximumDiscountAmount, coupon.Scope)
            : null;

        var pricing = PricingCalculator.Compute(
            merged.Select(x => new PricingLineInput(x.ItemType, x.ItemId, x.Name, x.Sku, x.ImageUrl, x.UnitPrice,
                x.Quantity, x.TaxRatePercent, x.HsnCode, x.VariantName)).ToList(),
            couponInput,
            shippingAmount,
            settings.PricesIncludeTax,
            settings.BusinessState,
            placeOfSupply ?? settings.BusinessState,
            settings.Currency);

        return new QuoteOutcome
        {
            Lines = merged,
            Pricing = pricing,
            Coupon = couponApplied ? coupon : null,
            CouponMessage = couponApplied ? "Coupon applied." : couponMessage,
            CouponApplied = couponApplied,
            HasPhysicalItems = hasPhysical,
            ShippingMethod = shipping,
            ShippingMethodName = shipping?.Name ?? "Studio appointment"
        };
    }

    public async Task<Order> CreateOrderAsync(Guid userId, CheckoutRequest request, QuoteOutcome quote, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken) ?? throw ApiException.Unauthorized();
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var orderNumber = await NextOrderNumberAsync(cancellationToken);
        var shippingAddress = JsonSerializer.Serialize(request.ShippingAddress);
        var billingAddress = request.BillingAddress is not null ? JsonSerializer.Serialize(request.BillingAddress) : shippingAddress;

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            CustomerEmail = user.Email ?? "",
            CustomerName = user.DisplayName,
            CustomerPhone = request.ShippingAddress.Phone,
            Status = OrderStatus.PaymentPending,
            PaymentStatus = PaymentStatus.Created,
            SubTotal = quote.Pricing.SubTotal,
            DiscountAmount = quote.Pricing.DiscountAmount,
            ShippingAmount = quote.Pricing.ShippingAmount,
            TaxAmount = quote.Pricing.TaxAmount,
            TotalAmount = quote.Pricing.TotalAmount,
            Currency = quote.Pricing.Currency,
            CouponCode = quote.Coupon?.Code,
            CouponId = quote.Coupon?.Id,
            Notes = request.Notes,
            ShippingAddressJson = shippingAddress,
            BillingAddressJson = billingAddress,
            ShippingMethodName = quote.ShippingMethodName,
            GstNumber = request.GstNumber,
            PlaceOfSupply = request.ShippingAddress.State,
            IsGstInvoice = settings.GstEnabled && !string.IsNullOrWhiteSpace(request.GstNumber),
            PlacedAtUtc = now,
            StatusHistory =
            [
                new OrderStatusHistory { Status = OrderStatus.PaymentPending, Note = "Order created; awaiting payment.", ChangedAtUtc = now }
            ]
        };

        foreach (var line in quote.Lines)
        {
            var priced = quote.Pricing.Lines.FirstOrDefault(x => x.Input.ItemType == line.ItemType && x.Input.ItemId == line.ItemId && x.Input.VariantName == line.VariantName);
            order.Items.Add(new OrderItem
            {
                ItemType = line.ItemType,
                ItemId = line.ItemId,
                Name = line.Name,
                Sku = line.Sku,
                ImageUrl = line.ImageUrl,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                TaxRatePercent = line.TaxRatePercent,
                TaxAmount = priced?.TaxAmount ?? 0m,
                LineTotal = priced?.LineTotal ?? line.UnitPrice * line.Quantity,
                HsnCode = line.HsnCode,
                VariantName = line.VariantName
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order {OrderNumber} created for {Email}", order.OrderNumber, order.CustomerEmail);
        return order;
    }

    public async Task<Order> CompletePaymentAsync(Guid orderId, string providerOrderId, string providerPaymentId, string? method, CancellationToken cancellationToken = default)
    {
        // SqlServerRetryingExecutionStrategy requires user-initiated transactions to run inside
        // the strategy so a transient failure can safely retry the whole unit of work.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var order = await db.Orders
                .Include(x => x.Items)
                .Include(x => x.Payments)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
                ?? throw ApiException.NotFound("Order");

            if (order.PaymentStatus == PaymentStatus.Captured)
            {
                await transaction.CommitAsync(cancellationToken);
                return order;
            }

            var payment = order.Payments.FirstOrDefault(p => p.ProviderOrderId == providerOrderId)
                          ?? order.Payments.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
            if (payment is null)
            {
                payment = new Payment
                {
                    OrderId = order.Id,
                    Provider = PaymentProviderType.Mock,
                    ProviderOrderId = providerOrderId,
                    Amount = order.TotalAmount,
                    Currency = order.Currency
                };
                order.Payments.Add(payment);
            }

            payment.ProviderPaymentId = providerPaymentId;
            payment.Status = PaymentStatus.Captured;
            payment.Method = method ?? "unknown";
            payment.PaidAtUtc = DateTime.UtcNow;
            payment.SafeSummary = "Payment captured and verified.";

            order.PaymentStatus = PaymentStatus.Captured;
            order.Status = OrderStatus.Paid;
            order.PaidAtUtc = DateTime.UtcNow;
            var paidHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderStatus.Paid,
                Note = "Payment captured.",
                ChangedAtUtc = DateTime.UtcNow
            };
            db.OrderStatusHistories.Add(paidHistory);
            order.StatusHistory.Add(paidHistory);

            await DecrementStockAsync(order, cancellationToken);
            await EnsureCouponRedemptionAsync(order, cancellationToken);
            await EnsureInvoiceAsync(order, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Payment completed for order {OrderNumber}", order.OrderNumber);
            return order;
        });
    }

    public async Task<int> DecrementStockAsync(Order order, CancellationToken cancellationToken = default)
    {
        var changed = 0;
        foreach (var item in order.Items)
        {
            if (item.ItemType == OrderItemType.Product)
            {
                var product = await db.Products.FirstOrDefaultAsync(x => x.Id == item.ItemId, cancellationToken);
                if (product is null) continue;
                if (!string.IsNullOrWhiteSpace(item.VariantName))
                {
                    var variant = await db.ProductVariants.FirstOrDefaultAsync(
                        x => x.ProductId == product.Id && x.Name == item.VariantName, cancellationToken);
                    if (variant is not null)
                    {
                        variant.StockQuantity = Math.Max(0, variant.StockQuantity - item.Quantity);
                        changed++;
                    }
                }
                product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                changed++;
            }
            else if (item.ItemType == OrderItemType.Combo)
            {
                var comboItems = await db.ProductComboItems
                    .Where(x => x.ComboId == item.ItemId && x.ProductId != null)
                    .ToListAsync(cancellationToken);
                foreach (var comboItem in comboItems)
                {
                    if (comboItem.ProductId is null) continue;
                    var product = await db.Products.FirstOrDefaultAsync(x => x.Id == comboItem.ProductId, cancellationToken);
                    if (product is null) continue;
                    product.StockQuantity = Math.Max(0, product.StockQuantity - comboItem.Quantity * item.Quantity);
                    changed++;
                }
            }
        }
        return changed;
    }

    private async Task EnsureCouponRedemptionAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.CouponId is null) return;
        var already = await db.CouponRedemptions.AnyAsync(x => x.OrderId == order.Id, cancellationToken);
        if (already) return;
        var coupon = await db.Coupons.FirstOrDefaultAsync(x => x.Id == order.CouponId, cancellationToken);
        if (coupon is null) return;
        coupon.UsedCount++;
        db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            UserId = order.UserId,
            OrderId = order.Id,
            DiscountAmount = order.DiscountAmount,
            RedeemedAtUtc = DateTime.UtcNow
        });
    }

    private async Task EnsureInvoiceAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.Invoice is not null) return;
        var number = await NextInvoiceNumberAsync(cancellationToken);
        var intraState = string.Equals(order.PlaceOfSupply, "Karnataka", StringComparison.OrdinalIgnoreCase);
        var invoice = new Invoice
        {
            OrderId = order.Id,
            InvoiceNumber = number,
            InvoiceDateUtc = DateTime.UtcNow,
            GstNumber = order.GstNumber,
            PlaceOfSupply = order.PlaceOfSupply,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            ShippingAmount = order.ShippingAmount,
            CgstAmount = intraState ? Math.Round(order.TaxAmount / 2m, 2, MidpointRounding.AwayFromZero) : 0m,
            SgstAmount = intraState ? order.TaxAmount - Math.Round(order.TaxAmount / 2m, 2, MidpointRounding.AwayFromZero) : 0m,
            IgstAmount = intraState ? 0m : order.TaxAmount,
            TotalAmount = order.TotalAmount
        };
        db.Invoices.Add(invoice);
        order.Invoice = invoice;
    }

    private async Task<string> NextOrderNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var count = await db.Orders.IgnoreQueryFilters().CountAsync(cancellationToken) + 1 + attempt;
            var candidate = $"MA-{year}-{count:0000}";
            if (!await db.Orders.IgnoreQueryFilters().AnyAsync(x => x.OrderNumber == candidate, cancellationToken))
                return candidate;
            if (attempt == 5)
                return $"MA-{year}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
        }
        return $"MA-{year}-{DateTime.UtcNow:HHmmss}";
    }

    /// <summary>
    /// Invoice numbers are sequential per calendar year. Seeded data can leave gaps, so take
    /// the highest existing suffix instead of counting rows. The unique index remains the
    /// final guard against a concurrent insert.
    /// </summary>
    private async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var numbers = await db.Invoices.IgnoreQueryFilters()
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .Select(x => x.InvoiceNumber)
            .ToListAsync(cancellationToken);
        var highest = 0;
        foreach (var value in numbers)
        {
            if (int.TryParse(value[prefix.Length..], out var parsed) && parsed > highest)
                highest = parsed;
        }
        return $"{prefix}{highest + 1:0000}";
    }
}
