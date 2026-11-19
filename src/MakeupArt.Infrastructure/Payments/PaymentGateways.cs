using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MakeupArt.Application.Common;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MakeupArt.Infrastructure.Payments;

public record PaymentInitResult(bool Success, PaymentProviderType Provider, string? ProviderOrderId, string? KeyId, string Message);
public record PaymentVerifyResult(bool Success, string? PaymentId, string? Method, string? FailureReason);
public record RefundResult(bool Success, string? ProviderRefundId, string? Message);

public interface IPaymentGateway
{
    PaymentProviderType Provider { get; }
    Task<PaymentInitResult> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<PaymentVerifyResult> VerifyAsync(string providerOrderId, string providerPaymentId, string? signature, CancellationToken cancellationToken = default);
    Task<RefundResult> RefundAsync(Payment payment, decimal amount, string reason, CancellationToken cancellationToken = default);
}

public interface IPaymentGatewayFactory
{
    IPaymentGateway Resolve(string? provider);
    IReadOnlyList<string> AvailableProviders { get; }
}

public class PaymentGatewayFactory(
    IEnumerable<IPaymentGateway> gateways,
    IOptions<PaymentOptions> paymentOptions,
    IOptions<RazorpayOptions> razorpayOptions,
    IHostEnvironment environment) : IPaymentGatewayFactory
{
    private readonly RazorpayOptions _razorpay = razorpayOptions.Value;
    private readonly bool _mockEnabled = environment.IsDevelopment() || environment.IsEnvironment("Testing");

    public IReadOnlyList<string> AvailableProviders => gateways
        .Where(IsAvailable)
        .Select(x => x.Provider.ToString())
        .ToList();

    public IPaymentGateway Resolve(string? provider)
    {
        var requested = string.IsNullOrWhiteSpace(provider) ? paymentOptions.Value.DefaultProvider : provider;
        if (!Enum.TryParse<PaymentProviderType>(requested, true, out var type))
            throw ApiException.Validation("Unknown payment provider.");
        if (type == PaymentProviderType.Mock && !_mockEnabled)
            throw ApiException.Validation("Mock payments are available only in development and automated tests.");
        if (type == PaymentProviderType.Razorpay && !_razorpay.IsConfigured)
            throw ApiException.Validation("Razorpay is not configured on this deployment.");
        var gateway = gateways.FirstOrDefault(x => x.Provider == type);
        if (gateway is null || gateway is UnavailablePaymentGateway)
            throw ApiException.Validation($"{type} payments are not enabled on this deployment.");
        return gateway;
    }

    private bool IsAvailable(IPaymentGateway gateway) => gateway.Provider switch
    {
        PaymentProviderType.Mock => _mockEnabled,
        PaymentProviderType.Razorpay => _razorpay.IsConfigured,
        _ => gateway is not UnavailablePaymentGateway
    };
}

public class MockPaymentGateway : IPaymentGateway
{
    public PaymentProviderType Provider => PaymentProviderType.Mock;

    public Task<PaymentInitResult> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentInitResult(true, Provider, $"mock_order_{order.Id:N}", "mock_key", "Mock sandbox payment is ready."));

    public Task<PaymentVerifyResult> VerifyAsync(string providerOrderId, string providerPaymentId, string? signature, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentVerifyResult(true, providerPaymentId, "mock_card", null));

    public Task<RefundResult> RefundAsync(Payment payment, decimal amount, string reason, CancellationToken cancellationToken = default)
        => Task.FromResult(new RefundResult(true, $"mock_refund_{Guid.NewGuid():N}", "Mock refund recorded."));
}

public class RazorpayPaymentGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<RazorpayOptions> options,
    ILogger<RazorpayPaymentGateway> logger) : IPaymentGateway
{
    private readonly RazorpayOptions _options = options.Value;
    public PaymentProviderType Provider => PaymentProviderType.Razorpay;

    public async Task<PaymentInitResult> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw ApiException.Validation("Razorpay is not configured. Add RAZORPAY__KEYID and RAZORPAY__KEYSECRET, or use the mock provider.");

        var client = CreateClient();
        var payload = new
        {
            amount = Convert.ToInt64(Math.Round(order.TotalAmount * 100, MidpointRounding.AwayFromZero)),
            currency = order.Currency,
            receipt = order.OrderNumber,
            notes = new { order_id = order.Id.ToString(), customer = order.CustomerEmail }
        };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("orders", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Razorpay order creation failed with {Status}: {Body}", response.StatusCode, body);
            return new PaymentInitResult(false, Provider, null, _options.KeyId, "Razorpay could not create a payment order.");
        }
        using var document = JsonDocument.Parse(body);
        var id = document.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        return new PaymentInitResult(!string.IsNullOrWhiteSpace(id), Provider, id, _options.KeyId, "Razorpay order created.");
    }

    public Task<PaymentVerifyResult> VerifyAsync(string providerOrderId, string providerPaymentId, string? signature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return Task.FromResult(new PaymentVerifyResult(false, providerPaymentId, null, "Payment signature is missing."));
        var expected = HmacSha256($"{providerOrderId}|{providerPaymentId}", _options.KeySecret);
        var valid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
        return Task.FromResult(valid
            ? new PaymentVerifyResult(true, providerPaymentId, "razorpay", null)
            : new PaymentVerifyResult(false, providerPaymentId, null, "Payment signature verification failed."));
    }

    public async Task<RefundResult> RefundAsync(Payment payment, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured) throw ApiException.Validation("Razorpay is not configured.");
        if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId)) throw ApiException.Validation("This payment has no Razorpay payment id.");
        var client = CreateClient();
        var payload = new
        {
            amount = Convert.ToInt64(Math.Round(amount * 100, MidpointRounding.AwayFromZero)),
            notes = new { reason },
            speed = "normal"
        };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"payments/{Uri.EscapeDataString(payment.ProviderPaymentId)}/refund", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Razorpay refund failed with {Status}: {Body}", response.StatusCode, body);
            return new RefundResult(false, null, "Razorpay rejected the refund request.");
        }
        using var document = JsonDocument.Parse(body);
        var id = document.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        return new RefundResult(true, id, "Refund submitted to Razorpay.");
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("razorpay");
        client.BaseAddress = new Uri("https://api.razorpay.com/v1/");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }

    public static string HmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}

public class UnavailablePaymentGateway(PaymentProviderType provider) : IPaymentGateway
{
    public PaymentProviderType Provider { get; } = provider;
    public Task<PaymentInitResult> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
        => throw ApiException.Validation($"{Provider} is not enabled. Configure credentials and the provider adapter before using it.");
    public Task<PaymentVerifyResult> VerifyAsync(string providerOrderId, string providerPaymentId, string? signature, CancellationToken cancellationToken = default)
        => throw ApiException.Validation($"{Provider} is not enabled.");
    public Task<RefundResult> RefundAsync(Payment payment, decimal amount, string reason, CancellationToken cancellationToken = default)
        => throw ApiException.Validation($"{Provider} is not enabled.");
}
