using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Options;
using MakeupArt.Infrastructure.Payments;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MakeupArt.Api.Controllers;

[Route("api/payments")]
public class PaymentsController(
    AppDbContext db,
    ICheckoutService checkout,
    IPaymentGatewayFactory gateways,
    IEmailService emailService,
    IOptions<RazorpayOptions> razorpayOptions,
    IOptions<PaymentOptions> paymentOptions,
    IHostEnvironment environment,
    ILogger<PaymentsController> logger) : BaseApiController
{
    private readonly RazorpayOptions _razorpay = razorpayOptions.Value;

    [AllowAnonymous]
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var defaultProvider = paymentOptions.Value.DefaultProvider;
        return Ok(new
        {
            defaultProvider,
            providers = gateways.AvailableProviders,
            razorpayKeyId = _razorpay.IsConfigured ? _razorpay.KeyId : null,
            razorpayEnabled = _razorpay.IsConfigured,
            demoMode = environment.IsDevelopment() || environment.IsEnvironment("Testing"),
            currency = "INR",
            methods = new[] { "upi", "card", "netbanking", "wallet" }
        });
    }

    [Authorize]
    [HttpPost("mock/complete")]
    [EnableRateLimiting("checkout")]
    public async Task<ActionResult<OrderDto>> CompleteMockPayment(MockPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw ApiException.Forbidden("Mock payments are disabled on this deployment.");

        var userId = RequireUserId();
        var order = await db.Orders.Include(x => x.Items).Include(x => x.Payments).Include(x => x.Invoice)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId, cancellationToken)
            ?? throw ApiException.NotFound("Order");

        if (order.PaymentStatus == PaymentStatus.Captured)
        {
            var already = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Payments)
                .Include(x => x.Invoice).Include(x => x.StatusHistory)
                .FirstAsync(x => x.Id == order.Id, cancellationToken);
            return already.ToDto();
        }

        var payment = order.Payments.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()
                      ?? throw ApiException.Validation("No payment is attached to this order.");
        if (payment.Provider != PaymentProviderType.Mock)
            throw ApiException.Forbidden("Mock payments are disabled for this order.");

        var providerOrderId = payment.ProviderOrderId ?? $"mock_order_{order.Id:N}";
        var providerPaymentId = $"mock_pay_{RandomNumberGenerator.GetHexString(12).ToLowerInvariant()}";
        var completed = await checkout.CompletePaymentAsync(order.Id, providerOrderId, providerPaymentId, request.Method ?? "mock_card", cancellationToken);

        await SendOrderConfirmationAsync(completed, cancellationToken);

        db.AuditLogs.Add(new MakeupArt.Domain.Identity.AuditLog
        {
            UserId = userId,
            Action = "payment.captured",
            EntityName = nameof(Order),
            EntityId = completed.Id.ToString(),
            Details = $"Mock payment captured for {completed.OrderNumber}.",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);

        var result = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Payments)
            .Include(x => x.Invoice).Include(x => x.StatusHistory)
            .FirstAsync(x => x.Id == completed.Id, cancellationToken);
        return result.ToDto();
    }

    [Authorize]
    [HttpPost("verify")]
    [EnableRateLimiting("checkout")]
    public async Task<ActionResult<OrderDto>> VerifyPayment(PaymentVerifyRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var order = await db.Orders.Include(x => x.Payments).ThenInclude(x => x.Refunds)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId, cancellationToken)
            ?? throw ApiException.NotFound("Order");
        var payment = order.Payments.FirstOrDefault(x => x.ProviderOrderId == request.ProviderOrderId)
                      ?? throw ApiException.NotFound("Payment");

        var gateway = gateways.Resolve(payment.Provider.ToString());
        var verification = await gateway.VerifyAsync(request.ProviderOrderId, request.ProviderPaymentId, request.Signature, cancellationToken);
        if (!verification.Success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = verification.FailureReason;
            order.PaymentStatus = PaymentStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            throw ApiException.Validation(verification.FailureReason ?? "Payment verification failed.");
        }

        var completed = await checkout.CompletePaymentAsync(order.Id, request.ProviderOrderId, request.ProviderPaymentId,
            request.Method ?? verification.Method, cancellationToken);
        await SendOrderConfirmationAsync(completed, cancellationToken);
        var result = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Payments)
            .Include(x => x.Invoice).Include(x => x.StatusHistory)
            .FirstAsync(x => x.Id == completed.Id, cancellationToken);
        return result.ToDto();
    }

    [AllowAnonymous]
    [HttpPost("webhook/razorpay")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RazorpayWebhook(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_razorpay.WebhookSecret))
            return NotFound();

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var expected = RazorpayPaymentGateway.HmacSha256(raw, _razorpay.WebhookSecret);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature)))
        {
            logger.LogWarning("Rejected Razorpay webhook with an invalid signature.");
            return Unauthorized();
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var eventName = root.TryGetProperty("event", out var eventElement) ? eventElement.GetString() : null;
        if (eventName is not ("payment.captured" or "order.paid")) return Ok();

        if (!root.TryGetProperty("payload", out var payload) ||
            !payload.TryGetProperty("payment", out var paymentPayload) ||
            !paymentPayload.TryGetProperty("entity", out var entity)) return Ok();

        var providerOrderId = entity.TryGetProperty("order_id", out var orderIdElement) ? orderIdElement.GetString() : null;
        var providerPaymentId = entity.TryGetProperty("id", out var paymentIdElement) ? paymentIdElement.GetString() : null;
        var method = entity.TryGetProperty("method", out var methodElement) ? methodElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(providerOrderId) || string.IsNullOrWhiteSpace(providerPaymentId)) return Ok();

        var payment = await db.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.ProviderOrderId == providerOrderId, cancellationToken);
        if (payment?.Order is null) return Ok();
        if (payment.Order.PaymentStatus == PaymentStatus.Captured) return Ok();

        await checkout.CompletePaymentAsync(payment.OrderId, providerOrderId, providerPaymentId, method, cancellationToken);
        logger.LogInformation("Razorpay webhook confirmed payment for order {OrderNumber}", payment.Order.OrderNumber);
        return Ok();
    }

    private async Task SendOrderConfirmationAsync(Order order, CancellationToken cancellationToken)
    {
        var lines = string.Join("", order.Items.Select(i =>
            $"<tr><td style=\"padding:6px 0\">{i.Name}{(string.IsNullOrWhiteSpace(i.VariantName) ? "" : $" · {i.VariantName}")} × {i.Quantity}</td><td style=\"text-align:right\">₹{i.LineTotal:N2}</td></tr>"));
        await emailService.SendAsync(order.CustomerEmail, $"Your Makeup Art order {order.OrderNumber}",
            $"""
             <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
               <h1 style="font-weight:400">Thank you, {order.CustomerName.Split(' ')[0]}.</h1>
               <p>Order <strong>{order.OrderNumber}</strong> is confirmed and being prepared with care.</p>
               <table style="width:100%;border-collapse:collapse;margin:20px 0">{lines}</table>
               <p style="display:flex;justify-content:space-between"><span>Total paid</span><strong>₹{order.TotalAmount:N2}</strong></p>
               <p>We will email tracking details as soon as your parcel leaves the studio.</p>
               <p style="font-size:13px;color:#7a666b">Questions? Reply to this email or contact care@makeupart.local.</p>
             </div>
             """, cancellationToken);
    }
}

[Route("api/checkout")]
public class CheckoutController(
    AppDbContext db,
    ICheckoutService checkout,
    IPaymentGatewayFactory gateways,
    IOptions<PaymentOptions> paymentOptions) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("quote")]
    [EnableRateLimiting("checkout")]
    public async Task<ActionResult<CheckoutQuoteDto>> Quote(CheckoutQuoteRequest request, CancellationToken cancellationToken)
    {
        var quote = await checkout.QuoteAsync(request.Items, request.CouponCode, request.ShippingMethodId,
            request.PlaceOfSupply, CurrentUserId, cancellationToken);
        return MapQuote(quote);
    }

    [Authorize]
    [HttpPost]
    [EnableRateLimiting("checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var provider = string.IsNullOrWhiteSpace(request.PaymentProvider)
            ? paymentOptions.Value.DefaultProvider
            : request.PaymentProvider;
        var gateway = gateways.Resolve(provider);
        var quote = await checkout.QuoteAsync(request.Items, request.CouponCode, request.ShippingMethodId,
            request.ShippingAddress.State, userId, cancellationToken);
        var order = await checkout.CreateOrderAsync(userId, request, quote, cancellationToken);
        var init = await gateway.CreateOrderAsync(order, cancellationToken);
        if (!init.Success)
        {
            order.Status = OrderStatus.Failed;
            order.PaymentStatus = PaymentStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            throw ApiException.Validation(init.Message);
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            Provider = init.Provider,
            ProviderOrderId = init.ProviderOrderId,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Status = PaymentStatus.Created,
            SafeSummary = init.Message
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        return new CheckoutResponse(
            order.Id,
            order.OrderNumber,
            order.TotalAmount,
            order.Currency,
            init.Provider.ToString(),
            init.ProviderOrderId,
            init.KeyId,
            init.Provider != MakeupArt.Domain.Common.PaymentProviderType.Mock,
            MapBreakdown(quote),
            quote.Lines.Select(line =>
            {
                var priced = quote.Pricing.Lines.FirstOrDefault(x => x.Input.ItemType == line.ItemType && x.Input.ItemId == line.ItemId && x.Input.VariantName == line.VariantName);
                return new CheckoutLineDto(line.ItemId, line.ItemType, line.Name, line.Sku, line.ImageUrl,
                    line.UnitPrice, line.Quantity, line.TaxRatePercent, priced?.TaxAmount ?? 0m, priced?.LineTotal ?? 0m,
                    line.HsnCode, line.VariantName);
            }).ToList());
    }

    private static CheckoutQuoteDto MapQuote(QuoteOutcome quote)
        => new(
            quote.Lines.Select(line =>
            {
                var priced = quote.Pricing.Lines.FirstOrDefault(x => x.Input.ItemType == line.ItemType && x.Input.ItemId == line.ItemId && x.Input.VariantName == line.VariantName);
                return new CheckoutQuoteLineDto(line.ItemId, line.ItemType, line.Name, line.ImageUrl, line.UnitPrice,
                    line.Quantity, line.TaxRatePercent, priced?.TaxAmount ?? 0m, priced?.LineTotal ?? 0m, line.HsnCode, line.VariantName);
            }).ToList(),
            MapBreakdown(quote),
            new CouponResultDto(quote.CouponApplied, quote.Coupon?.Code, quote.CouponMessage, quote.Pricing.DiscountAmount),
            quote.HasPhysicalItems,
            quote.ShippingMethod?.Id,
            quote.ShippingMethodName);

    private static PriceBreakdownDto MapBreakdown(QuoteOutcome quote)
        => new(quote.Pricing.SubTotal, quote.Pricing.DiscountAmount, quote.Pricing.TaxAmount, quote.Pricing.ShippingAmount,
            quote.Pricing.TotalAmount, quote.Pricing.CgstAmount, quote.Pricing.SgstAmount, quote.Pricing.IgstAmount,
            quote.Pricing.Currency, quote.Pricing.PricesIncludeTax);
}
