using MakeupArt.Application.Common;
using MakeupArt.Domain.Common;

namespace MakeupArt.Application.Pricing;

public sealed record PricingLineInput(
    OrderItemType ItemType,
    Guid ItemId,
    string Name,
    string? Sku,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal TaxRatePercent,
    string? HsnCode = null,
    string? VariantName = null);

public sealed record PricedLine(
    PricingLineInput Input,
    decimal UnitPrice,
    decimal Quantity,
    decimal TaxableAmount,
    decimal TaxAmount,
    decimal DiscountShare,
    decimal LineTotal);

public sealed record CouponInput(string Code, DiscountType DiscountType, decimal Value, decimal MinimumOrderAmount, decimal? MaximumDiscountAmount, CouponScope Scope);

public sealed class PricingResult
{
    public IReadOnlyList<PricedLine> Lines { get; init; } = [];
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal CgstAmount { get; init; }
    public decimal SgstAmount { get; init; }
    public decimal IgstAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public string Currency { get; init; } = "INR";
    public bool PricesIncludeTax { get; init; }
}

public static class PricingCalculator
{
    public static PricingResult Compute(
        IReadOnlyCollection<PricingLineInput> inputs,
        CouponInput? coupon,
        decimal shippingAmount,
        bool pricesIncludeTax,
        string businessState,
        string placeOfSupply,
        string currency = "INR")
    {
        if (inputs.Count == 0)
            throw new ApiException("Your cart is empty.", 422);

        var initial = inputs.Select(input =>
        {
            var gross = Money.Round(input.UnitPrice * input.Quantity);
            var rate = input.TaxRatePercent / 100m;
            var net = pricesIncludeTax ? Money.Round(gross / (1 + rate)) : gross;
            var tax = pricesIncludeTax ? Money.Round(gross - net) : Money.Round(net * rate);
            return new WorkingLine(input, net, tax, gross, 0m);
        }).ToList();

        var subtotal = pricesIncludeTax
            ? Money.Round(initial.Sum(x => x.Gross))
            : Money.Round(initial.Sum(x => x.Net));

        var discount = 0m;
        if (coupon is not null && subtotal >= coupon.MinimumOrderAmount)
        {
            var eligible = initial.Where(x => IsEligible(x.Input.ItemType, coupon.Scope)).ToList();
            var eligibleNet = Money.Round(eligible.Sum(x => x.Net));
            if (eligibleNet > 0)
            {
                discount = coupon.DiscountType == DiscountType.Percentage
                    ? Money.Round(eligibleNet * coupon.Value / 100m)
                    : coupon.Value;
                if (coupon.MaximumDiscountAmount is > 0)
                    discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);
                discount = Math.Min(discount, eligibleNet);
                discount = Money.Round(Math.Max(0m, discount));

                // Allocate the discount proportionally across the eligible lines.
                var remaining = discount;
                for (var i = 0; i < eligible.Count; i++)
                {
                    var line = eligible[i];
                    var share = i == eligible.Count - 1
                        ? remaining
                        : Money.Round(discount * (line.Net / eligibleNet));
                    share = Math.Min(share, line.Net);
                    remaining = Money.Round(remaining - share);
                    line.DiscountShare = share;
                }
                if (remaining != 0 && eligible.Count > 0)
                {
                    var last = eligible[^1];
                    last.DiscountShare = Math.Min(last.Net, Money.Round(last.DiscountShare + remaining));
                    discount = eligible.Sum(x => x.DiscountShare);
                }
            }
        }

        var priced = new List<PricedLine>(initial.Count);
        foreach (var line in initial)
        {
            var taxable = Money.Round(Math.Max(0m, line.Net - line.DiscountShare));
            var tax = Money.Round(taxable * line.Input.TaxRatePercent / 100m);
            var lineTotal = Money.Round(taxable + tax);
            priced.Add(new PricedLine(
                line.Input,
                line.Input.UnitPrice,
                line.Input.Quantity,
                taxable,
                tax,
                line.DiscountShare,
                lineTotal));
        }

        var taxAmount = Money.Round(priced.Sum(x => x.TaxAmount));
        var taxableAmount = Money.Round(priced.Sum(x => x.TaxableAmount));
        var total = pricesIncludeTax
            ? Money.Round(subtotal - discount + shippingAmount)
            : Money.Round(taxableAmount + taxAmount + shippingAmount);

        // Reconcile rounding to the line totals.
        var lineTotalSum = Money.Round(priced.Sum(x => x.LineTotal));
        if (lineTotalSum + shippingAmount != total && priced.Count > 0)
        {
            var diff = Money.Round(total - shippingAmount - lineTotalSum);
            if (diff != 0)
            {
                var last = priced[^1];
                priced[^1] = last with
                {
                    LineTotal = Money.Round(last.LineTotal + diff),
                    TaxAmount = Money.Round(last.TaxAmount + diff)
                };
                taxAmount = Money.Round(taxAmount + diff);
            }
        }

        var intraState = string.Equals(businessState, placeOfSupply, StringComparison.OrdinalIgnoreCase);
        var cgst = intraState ? Money.Round(taxAmount / 2m) : 0m;
        var sgst = intraState ? Money.Round(taxAmount - cgst) : 0m;
        var igst = intraState ? 0m : taxAmount;

        return new PricingResult
        {
            Lines = priced,
            SubTotal = subtotal,
            DiscountAmount = Money.Round(discount),
            TaxAmount = taxAmount,
            ShippingAmount = Money.Round(shippingAmount),
            TotalAmount = Math.Max(0m, total),
            CgstAmount = cgst,
            SgstAmount = sgst,
            IgstAmount = igst,
            TaxableAmount = taxableAmount,
            Currency = currency,
            PricesIncludeTax = pricesIncludeTax
        };
    }

    private static bool IsEligible(OrderItemType type, CouponScope scope) => scope switch
    {
        CouponScope.All => true,
        CouponScope.Products => type is OrderItemType.Product or OrderItemType.Combo,
        CouponScope.Services => type is OrderItemType.Service,
        CouponScope.Packages => type is OrderItemType.Package,
        _ => false
    };

    private sealed class WorkingLine(PricingLineInput input, decimal net, decimal tax, decimal gross, decimal discountShare)
    {
        public PricingLineInput Input { get; } = input;
        public decimal Net { get; } = net;
        public decimal Tax { get; } = tax;
        public decimal Gross { get; } = gross;
        public decimal DiscountShare { get; set; } = discountShare;
    }
}
