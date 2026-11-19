using MakeupArt.Domain.Common;
using MakeupArt.Domain.Identity;

namespace MakeupArt.Domain.Commerce;

public class CustomerAddress : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Label { get; set; } = "Home";
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }
}

public class TaxRate : BaseEntity
{
    public string Name { get; set; } = "GST 18%";
    public decimal RatePercent { get; set; } = 18m;
    public string Country { get; set; } = "India";
    public string? State { get; set; }
    public string? HsnCode { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}

public class ShippingMethod : BaseEntity
{
    public string Name { get; set; } = "Standard delivery";
    public string? Description { get; set; }
    public decimal Price { get; set; } = 99m;
    public decimal? FreeAboveAmount { get; set; }
    public string EstimatedDays { get; set; } = "3–5 business days";
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Created;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? CouponCode { get; set; }
    public Guid? CouponId { get; set; }
    public string? Notes { get; set; }
    public string ShippingAddressJson { get; set; } = "{}";
    public string BillingAddressJson { get; set; } = "{}";
    public string ShippingMethodName { get; set; } = "Standard delivery";
    public string? TrackingNumber { get; set; }
    public string? GstNumber { get; set; }
    public string? PlaceOfSupply { get; set; }
    public bool IsGstInvoice { get; set; }
    public DateTime? PlacedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? AdminNotes { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
    public List<Refund> Refunds { get; set; } = [];
    public Invoice? Invoice { get; set; }
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderItemType ItemType { get; set; } = OrderItemType.Product;
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TaxRatePercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? HsnCode { get; set; }
    public string? VariantName { get; set; }
}

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public PaymentProviderType Provider { get; set; } = PaymentProviderType.Mock;
    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderSignature { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;
    public string? Method { get; set; }
    public string? FailureReason { get; set; }
    public string? SafeSummary { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public List<Refund> Refunds { get; set; } = [];
}

public class Refund : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ProviderRefundId { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public DateTime? ProcessedAtUtc { get; set; }
    public Guid? ProcessedByUserId { get; set; }
}

public class Invoice : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDateUtc { get; set; } = DateTime.UtcNow;
    public string? GstNumber { get; set; }
    public string? PlaceOfSupply { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PdfUrl { get; set; }
}

public class BookingRequest : BaseEntity
{
    public string BookingNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
    public Catalog.Service? Service { get; set; }
    public Guid? PackageId { get; set; }
    public Catalog.ServicePackage? Package { get; set; }
    public DateTime PreferredDate { get; set; }
    public string PreferredTime { get; set; } = "10:00";
    public string? Occasion { get; set; }
    public string? Notes { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? AdminNotes { get; set; }
}

public class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ContactStatus Status { get; set; } = ContactStatus.New;
    public string? AdminNotes { get; set; }
}

public class SupportTicket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;
    public List<SupportMessage> Messages { get; set; } = [];
}

public class SupportMessage : BaseEntity
{
    public Guid TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }
    public Guid? SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public SenderType SenderType { get; set; } = SenderType.Customer;
    public string Body { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsInternal { get; set; }
}
