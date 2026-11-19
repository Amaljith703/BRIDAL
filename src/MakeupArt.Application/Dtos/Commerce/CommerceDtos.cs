using System.ComponentModel.DataAnnotations;
using MakeupArt.Domain.Common;

namespace MakeupArt.Application.Dtos.Commerce;

public class AddressRequest
{
    public string Label { get; set; } = "Home";
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, Phone, MaxLength(30)] public string Phone { get; set; } = string.Empty;
    [Required, MaxLength(250)] public string AddressLine1 { get; set; } = string.Empty;
    [MaxLength(250)] public string? AddressLine2 { get; set; }
    [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string State { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string PostalCode { get; set; } = string.Empty;
    [MaxLength(100)] public string Country { get; set; } = "India";
    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }
}

public record AddressDto(Guid Id, string Label, string FullName, string Phone, string AddressLine1, string? AddressLine2, string City, string State, string PostalCode, string Country, bool IsDefaultShipping, bool IsDefaultBilling);

public class CartItemRequest
{
    [Required] public OrderItemType ItemType { get; set; }
    [Required] public Guid ItemId { get; set; }
    [Range(1, 20)] public int Quantity { get; set; } = 1;
    public string? VariantName { get; set; }
}

public class CheckoutRequest
{
    [Required, MinLength(1)] public List<CartItemRequest> Items { get; set; } = [];
    [Required] public AddressRequest ShippingAddress { get; set; } = new();
    public AddressRequest? BillingAddress { get; set; }
    public Guid? ShippingMethodId { get; set; }
    public string? CouponCode { get; set; }
    public string? GstNumber { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public string? PaymentProvider { get; set; }
}

public record CheckoutLineDto(Guid ItemId, OrderItemType ItemType, string Name, string? Sku, string? ImageUrl, decimal UnitPrice, int Quantity, decimal TaxRatePercent, decimal TaxAmount, decimal LineTotal, string? HsnCode, string? VariantName);
public record PriceBreakdownDto(decimal SubTotal, decimal DiscountAmount, decimal TaxAmount, decimal ShippingAmount, decimal TotalAmount, decimal CgstAmount, decimal SgstAmount, decimal IgstAmount, string Currency, bool PricesIncludeTax);
public record CheckoutResponse(Guid OrderId, string OrderNumber, decimal Amount, string Currency, string Provider, string? ProviderOrderId, string? ProviderKeyId, bool RequiresPayment, PriceBreakdownDto Breakdown, IReadOnlyList<CheckoutLineDto> Lines);

public class CheckoutQuoteRequest
{
    [Required, MinLength(1)] public List<CartItemRequest> Items { get; set; } = [];
    public Guid? ShippingMethodId { get; set; }
    public string? CouponCode { get; set; }
    public string? PlaceOfSupply { get; set; }
}
public record CheckoutQuoteLineDto(Guid ItemId, OrderItemType ItemType, string Name, string? ImageUrl, decimal UnitPrice, int Quantity, decimal TaxRatePercent, decimal TaxAmount, decimal LineTotal, string? HsnCode, string? VariantName);
public record CouponResultDto(bool Applied, string? Code, string? Message, decimal DiscountAmount);
public record CheckoutQuoteDto(IReadOnlyList<CheckoutQuoteLineDto> Lines, PriceBreakdownDto Breakdown, CouponResultDto Coupon, bool HasPhysicalItems, Guid? ShippingMethodId, string ShippingMethodName);

public class PaymentVerifyRequest
{
    [Required] public Guid OrderId { get; set; }
    [Required] public string ProviderOrderId { get; set; } = string.Empty;
    [Required] public string ProviderPaymentId { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string? Method { get; set; }
}

public class MockPaymentRequest
{
    [Required] public Guid OrderId { get; set; }
    public string? Method { get; set; } = "mock_card";
}

public record OrderItemDto(Guid Id, OrderItemType ItemType, Guid ItemId, string Name, string? Sku, string? ImageUrl, decimal UnitPrice, int Quantity, decimal TaxRatePercent, decimal TaxAmount, decimal LineTotal, string? VariantName);
public record PaymentDto(Guid Id, string Provider, string? ProviderOrderId, string? ProviderPaymentId, decimal Amount, string Currency, string Status, string? Method, DateTime? PaidAtUtc, DateTime CreatedAtUtc);
public record OrderSummaryDto(Guid Id, string OrderNumber, string CustomerName, string CustomerEmail, OrderStatus Status, PaymentStatus PaymentStatus, decimal TotalAmount, string Currency, int ItemCount, DateTime CreatedAtUtc, DateTime? PaidAtUtc);
public record OrderDto(Guid Id, string OrderNumber, string CustomerName, string CustomerEmail, string CustomerPhone, OrderStatus Status, PaymentStatus PaymentStatus, decimal SubTotal, decimal DiscountAmount, decimal ShippingAmount, decimal TaxAmount, decimal TotalAmount, string Currency, string? CouponCode, string? Notes, string ShippingMethodName, string? TrackingNumber, string? GstNumber, string? PlaceOfSupply, bool IsGstInvoice, DateTime? PlacedAtUtc, DateTime? PaidAtUtc, DateTime CreatedAtUtc, string ShippingAddressJson, string BillingAddressJson, IReadOnlyList<OrderItemDto> Items, IReadOnlyList<PaymentDto> Payments, string? InvoiceNumber, IReadOnlyList<OrderStatusEventDto> StatusHistory);
public record OrderStatusEventDto(OrderStatus Status, string? Note, DateTime ChangedAtUtc);

public class ReviewCreateRequest
{
    [Range(1, 5)] public int Rating { get; set; } = 5;
    [Required, MaxLength(150)] public string Title { get; set; } = string.Empty;
    [Required, MinLength(10), MaxLength(3000)] public string Body { get; set; } = string.Empty;
}

public class BookingCreateRequest
{
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, Phone, MaxLength(30)] public string Phone { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
    public Guid? PackageId { get; set; }
    [Required] public DateTime PreferredDate { get; set; }
    [Required, MaxLength(20)] public string PreferredTime { get; set; } = "10:00";
    [MaxLength(100)] public string? Occasion { get; set; }
    [MaxLength(1500)] public string? Notes { get; set; }
}
public record BookingDto(Guid Id, string BookingNumber, string FullName, string Email, string Phone, string? ServiceName, string? PackageName, DateTime PreferredDate, string PreferredTime, string? Occasion, string? Notes, string Status, string? AdminNotes, DateTime CreatedAtUtc);

public class ContactRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Phone, MaxLength(30)] public string? Phone { get; set; }
    [Required, MaxLength(200)] public string Subject { get; set; } = string.Empty;
    [Required, MinLength(10), MaxLength(5000)] public string Message { get; set; } = string.Empty;
}

public class SupportTicketCreateRequest
{
    [Required, MaxLength(200)] public string Subject { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Category { get; set; } = "Order";
    [Required, MinLength(10), MaxLength(5000)] public string Message { get; set; } = string.Empty;
}

public class SupportReplyRequest
{
    [Required, MinLength(1), MaxLength(5000)] public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public record SupportMessageDto(Guid Id, string SenderName, string SenderType, string Body, string? AttachmentUrl, bool IsInternal, DateTime CreatedAtUtc);
public record SupportTicketSummaryDto(Guid Id, string TicketNumber, string Subject, string Category, string Priority, string Status, int MessageCount, DateTime LastMessageAtUtc, DateTime CreatedAtUtc);
public record SupportTicketDto(Guid Id, string TicketNumber, Guid UserId, string? CustomerName, string? CustomerEmail, string Subject, string Category, string Priority, string Status, DateTime LastMessageAtUtc, DateTime CreatedAtUtc, IReadOnlyList<SupportMessageDto> Messages);

public class AnalyticsEventRequest
{
    [Required, MaxLength(200)] public string EventType { get; set; } = "page_view";
    [Required, MaxLength(500)] public string Path { get; set; } = "/";
    public string? SessionId { get; set; }
    public string? Referrer { get; set; }
    public string? DeviceType { get; set; }
    public string? MetadataJson { get; set; }
}

public class RefundRequest
{
    [Range(0.01, 10000000)] public decimal Amount { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = string.Empty;
}

public class OrderStatusUpdateRequest
{
    [Required] public OrderStatus Status { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
    public string? TrackingNumber { get; set; }
}
