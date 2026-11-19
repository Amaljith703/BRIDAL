using MakeupArt.Application.Common;
using MakeupArt.Application.Pricing;
using MakeupArt.Domain.Common;

namespace MakeupArt.UnitTests;

public sealed class PricingCalculatorTests
{
    [Fact]
    public void Compute_AddsTaxAndSplitsItForAnIntraStateOrder()
    {
        var result = PricingCalculator.Compute(
            [Line(OrderItemType.Product, 100m, 2, 18m)],
            coupon: null,
            shippingAmount: 30m,
            pricesIncludeTax: false,
            businessState: "Karnataka",
            placeOfSupply: "KARNATAKA");

        Assert.Equal(200m, result.SubTotal);
        Assert.Equal(36m, result.TaxAmount);
        Assert.Equal(18m, result.CgstAmount);
        Assert.Equal(18m, result.SgstAmount);
        Assert.Equal(0m, result.IgstAmount);
        Assert.Equal(30m, result.ShippingAmount);
        Assert.Equal(266m, result.TotalAmount);
    }

    [Fact]
    public void ComputeAppliesProductCouponBeforeTaxAndChargesInterStateIgst()
    {
        var coupon = new CouponInput("GLOW20", DiscountType.Percentage, 20m, 0m, 50m, CouponScope.Products);
        var result = PricingCalculator.Compute(
            [Line(OrderItemType.Product, 100m, 1, 18m), Line(OrderItemType.Service, 200m, 1, 18m)],
            coupon,
            shippingAmount: 0m,
            pricesIncludeTax: false,
            businessState: "Karnataka",
            placeOfSupply: "Tamil Nadu");

        Assert.Equal(300m, result.SubTotal);
        Assert.Equal(20m, result.DiscountAmount);
        Assert.Equal(50.40m, result.TaxAmount);
        Assert.Equal(0m, result.CgstAmount);
        Assert.Equal(0m, result.SgstAmount);
        Assert.Equal(50.40m, result.IgstAmount);
        Assert.Equal(330.40m, result.TotalAmount);
        Assert.Equal(80m, result.Lines[0].TaxableAmount);
        Assert.Equal(200m, result.Lines[1].TaxableAmount);
    }

    [Fact]
    public void ComputeDoesNotAddInclusiveTaxTwice()
    {
        var result = PricingCalculator.Compute(
            [Line(OrderItemType.Product, 118m, 1, 18m)],
            coupon: null,
            shippingAmount: 0m,
            pricesIncludeTax: true,
            businessState: "Karnataka",
            placeOfSupply: "Karnataka");

        Assert.Equal(118m, result.SubTotal);
        Assert.Equal(100m, result.TaxableAmount);
        Assert.Equal(18m, result.TaxAmount);
        Assert.Equal(118m, result.TotalAmount);
        Assert.True(result.PricesIncludeTax);
    }

    [Fact]
    public void ComputeRejectsAnEmptyCart()
    {
        var exception = Assert.Throws<ApiException>(() => PricingCalculator.Compute(
            [], coupon: null, shippingAmount: 0m, pricesIncludeTax: true,
            businessState: "Karnataka", placeOfSupply: "Karnataka"));

        Assert.Equal(422, exception.StatusCode);
    }

    private static PricingLineInput Line(OrderItemType type, decimal unitPrice, int quantity, decimal taxRate) =>
        new(type, Guid.NewGuid(), "Studio essential", "MA-001", null, unitPrice, quantity, taxRate);
}
