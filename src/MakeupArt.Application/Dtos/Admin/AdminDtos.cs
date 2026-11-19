using System.ComponentModel.DataAnnotations;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Domain.Common;

namespace MakeupArt.Application.Dtos.Admin;

public record KpiDto(string Label, decimal Value, string? Detail, string Format);
public record ChartPointDto(string Label, decimal Value);
public record TopItemDto(Guid? Id, string Name, int Quantity, decimal Revenue, string? ImageUrl);
public record StatusSliceDto(string Status, int Count, decimal Value);
public record RecentOrderDto(Guid Id, string OrderNumber, string CustomerName, decimal TotalAmount, string Status, DateTime CreatedAtUtc);
public record DashboardDto(
    decimal RevenueToday, decimal Revenue7d, decimal Revenue30d, int OrdersToday, int Orders30d,
    decimal AverageOrderValue, int TotalCustomers, int NewCustomers30d, int PageViews30d,
    int PendingOrders, int LowStockCount, int PendingReviews, int PendingBookings, int OpenTickets,
    IReadOnlyList<ChartPointDto> RevenueSeries, IReadOnlyList<ChartPointDto> OrderSeries,
    IReadOnlyList<TopItemDto> TopProducts, IReadOnlyList<TopItemDto> TopServices,
    IReadOnlyList<StatusSliceDto> OrderStatuses, IReadOnlyList<StatusSliceDto> PaymentMethods,
    IReadOnlyList<RecentOrderDto> RecentOrders, string? AiInsight);

public class HeroSlideRequest
{
    [Required, MaxLength(200)] public string Eyebrow { get; set; } = "";
    [Required, MaxLength(250)] public string Title { get; set; } = "";
    [Required, MaxLength(500)] public string Subtitle { get; set; } = "";
    public string? ModelImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public string? SecondaryCtaLabel { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public string AccentColor { get; set; } = "#E9C8C1";
    public string SecondaryColor { get; set; } = "#C9A26B";
    public string? EffectJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SocialLinkRequest
{
    [Required, MaxLength(80)] public string Platform { get; set; } = "";
    [Required, Url, MaxLength(500)] public string Url { get; set; } = "";
    [MaxLength(80)] public string Icon { get; set; } = "link";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MusicTrackRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(200)] public string Artist { get; set; } = "Makeup Art Studio";
    [Required, MaxLength(500)] public string Url { get; set; } = "";
    public int DurationSeconds { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public string? Credit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BannerRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    [Required, MaxLength(500)] public string ImageUrl { get; set; } = "";
    public string? LinkUrl { get; set; }
    [MaxLength(50)] public string Placement { get; set; } = "home";
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TestimonialRequest
{
    [Required, MaxLength(150)] public string CustomerName { get; set; } = "";
    [MaxLength(150)] public string? Role { get; set; }
    [Required, MaxLength(2000)] public string Quote { get; set; } = "";
    [Range(1, 5)] public int Rating { get; set; } = 5;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsFeatured { get; set; }
    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FaqRequest
{
    [Required, MaxLength(500)] public string Question { get; set; } = "";
    [Required, MaxLength(5000)] public string Answer { get; set; } = "";
    [MaxLength(80)] public string Category { get; set; } = "General";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PortfolioItemRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(80)] public string Category { get; set; } = "Bridal";
    public string? Description { get; set; }
    [Required, MaxLength(500)] public string ImageUrl { get; set; } = "";
    public string? VideoUrl { get; set; }
    public string? TagsCsv { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class VlogRequest
{
    [Required, MaxLength(250)] public string Title { get; set; } = "";
    [Required, MaxLength(150)] public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public VlogType Type { get; set; } = VlogType.Makeup;
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int DurationSeconds { get; set; }
    public string? InfluencerName { get; set; }
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PageRequest
{
    [Required, MaxLength(120)] public string Key { get; set; } = "";
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ContentSectionRequest
{
    [Required, MaxLength(120)] public string Key { get; set; } = "";
    [Required, MaxLength(80)] public string SectionType { get; set; } = "text";
    public string? Eyebrow { get; set; }
    public string? Heading { get; set; }
    public string? Subheading { get; set; }
    public string? BodyHtml { get; set; }
    public string? MediaUrl { get; set; }
    public string? SecondaryMediaUrl { get; set; }
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public string? ConfigJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CategoryRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(150)] public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductImageRequest
{
    [Required] public string Url { get; set; } = "";
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
}

public class ProductVariantRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(80)] public string Sku { get; set; } = "";
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

public class ProductRequest
{
    [Required, MaxLength(250)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string Sku { get; set; } = "";
    [MaxLength(150)] public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    [MaxLength(120)] public string? Brand { get; set; } = "Makeup Art";
    [Range(0, 10000000)] public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public Guid? TaxRateId { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsFeatured { get; set; }
    public bool IsPromoted { get; set; }
    public bool IsNewArrival { get; set; }
    public string? ImageUrl { get; set; }
    public string? TagsCsv { get; set; }
    public int WeightGrams { get; set; } = 250;
    [MaxLength(20)] public string? HsnCode { get; set; } = "3304";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductImageRequest> Images { get; set; } = [];
    public List<ProductVariantRequest> Variants { get; set; } = [];
}

public class ServiceRequest
{
    [Required, MaxLength(250)] public string Name { get; set; } = "";
    [MaxLength(150)] public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    [Range(0, 10000000)] public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    [Range(0, 1440)] public int DurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? TaxRateId { get; set; }
    [MaxLength(20)] public string? HsnCode { get; set; } = "9997";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PackageItemRequest
{
    [Required] public Guid ServiceId { get; set; }
    [Range(1, 20)] public int Quantity { get; set; } = 1;
    public int SortOrder { get; set; }
}

public class ServicePackageRequest
{
    [Required, MaxLength(250)] public string Name { get; set; } = "";
    [MaxLength(150)] public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    [Range(0, 10000000)] public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    [MaxLength(80)] public string? Occasion { get; set; } = "Bridal";
    public bool IsFeatured { get; set; }
    public Guid? TaxRateId { get; set; }
    public string? HsnCode { get; set; } = "9997";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PackageItemRequest> Items { get; set; } = [];
}

public class ComboItemRequest
{
    public Guid? ProductId { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? PackageId { get; set; }
    [Range(1, 20)] public int Quantity { get; set; } = 1;
    public int SortOrder { get; set; }
}

public class ComboRequest
{
    [Required, MaxLength(250)] public string Name { get; set; } = "";
    [MaxLength(150)] public string? Slug { get; set; }
    public string? Description { get; set; }
    [Range(0, 10000000)] public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ComboItemRequest> Items { get; set; } = [];
}

public class OfferRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    [MaxLength(40)] public string? Code { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public string? ImageUrl { get; set; }
    public DiscountAppliesTo AppliesTo { get; set; } = DiscountAppliesTo.All;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CouponRequest
{
    [Required, MaxLength(40)] public string Code { get; set; } = "";
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    [Range(0.01, 10000000)] public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public CouponScope Scope { get; set; } = CouponScope.All;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddMonths(3);
    public int UsageLimit { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class ShippingMethodRequest
{
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Range(0, 100000)] public decimal Price { get; set; }
    public decimal? FreeAboveAmount { get; set; }
    [MaxLength(120)] public string EstimatedDays { get; set; } = "3–5 business days";
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TaxRateRequest
{
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Range(0, 100)] public decimal RatePercent { get; set; } = 18m;
    [MaxLength(80)] public string Country { get; set; } = "India";
    public string? State { get; set; }
    [MaxLength(20)] public string? HsnCode { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReviewModerationRequest
{
    [Required] public ReviewStatus Status { get; set; }
    [MaxLength(2000)] public string? AdminResponse { get; set; }
}

public class BookingStatusUpdateRequest
{
    [Required] public BookingStatus Status { get; set; }
    public string? AdminNotes { get; set; }
}

public class ContactStatusUpdateRequest
{
    [Required] public ContactStatus Status { get; set; }
    public string? AdminNotes { get; set; }
}

public class UserAdminUpdateRequest
{
    public bool IsActive { get; set; } = true;
    public string? AdminNotes { get; set; }
    [MaxLength(80)] public string? Role { get; set; }
}

public record AdminUserDto(Guid Id, string Email, string DisplayName, string? Phone, bool IsActive, bool EmailConfirmed, bool TwoFactorEnabled, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc, IReadOnlyList<string> Roles, int OrderCount, decimal TotalSpend);
public record ContactMessageDto(Guid Id, string Name, string Email, string? Phone, string Subject, string Message, string Status, string? AdminNotes, DateTime CreatedAtUtc);
public record CustomerAddressAdminDto(Guid Id, string Label, string FullName, string Phone, string AddressLine1, string? AddressLine2, string City, string State, string PostalCode, string Country, bool IsDefaultShipping);
public record AdminReviewItemDto(Guid Id, Guid ProductId, string ProductName, string? UserName, string? UserEmail, int Rating, string Title, string Body, string Status, bool IsVerifiedPurchase, string? AdminResponse, DateTime CreatedAtUtc);
public record AuditLogDto(Guid Id, string Action, string EntityName, string? EntityId, string? Details, string? UserEmail, string? IpAddress, DateTime CreatedAtUtc);
public record AdminRefundDto(Guid Id, decimal Amount, string Reason, string Status, string? ProviderRefundId, DateTime? ProcessedAtUtc, DateTime CreatedAtUtc);
public record AdminOrderDto(
    Guid Id, string OrderNumber, string CustomerName, string CustomerEmail, string CustomerPhone,
    string Status, string PaymentStatus, decimal SubTotal, decimal DiscountAmount, decimal ShippingAmount,
    decimal TaxAmount, decimal TotalAmount, string Currency, string? CouponCode, string? Notes, string? AdminNotes,
    string ShippingMethodName, string? TrackingNumber, string? GstNumber, string? PlaceOfSupply, bool IsGstInvoice,
    DateTime? PlacedAtUtc, DateTime? PaidAtUtc, DateTime CreatedAtUtc, string ShippingAddressJson, string BillingAddressJson,
    IReadOnlyList<OrderItemDto> Items, IReadOnlyList<PaymentDto> Payments, IReadOnlyList<AdminRefundDto> Refunds,
    string? InvoiceNumber, IReadOnlyList<OrderStatusEventDto> StatusHistory);
public record AdminCustomerDetailDto(AdminUserDto User, string? AdminNotes, IReadOnlyList<CustomerAddressAdminDto> Addresses, IReadOnlyList<OrderSummaryDto> RecentOrders);
public class AdminNotesRequest
{
    [MaxLength(2000)] public string? AdminNotes { get; set; }
}
public class SupportTicketUpdateRequest
{
    [Required] public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority? Priority { get; set; }
}
public class RefundStatusUpdateRequest
{
    [Required] public RefundStatus Status { get; set; }
    [MaxLength(120)] public string? ProviderRefundId { get; set; }
}
public record BackupRecordDto(Guid Id, string FileName, long SizeBytes, DateTime StartedAtUtc, DateTime? CompletedAtUtc, string Status, string? Message, string BackupType);
public record AnalyticsDashboardDto(
    int PageViews, int UniqueSessions, int ProductViews, int AddToCarts, int Checkouts, decimal ConversionRate,
    IReadOnlyList<ChartPointDto> PageViewSeries, IReadOnlyList<ChartPointDto> TopPages,
    IReadOnlyList<ChartPointDto> TrafficSources, IReadOnlyList<ChartPointDto> DeviceSplit,
    IReadOnlyList<ChartPointDto> EventBreakdown);
public class ReorderRequest
{
    [Required] public List<ReorderItem> Items { get; set; } = [];
}
public record ReorderItem(Guid Id, int SortOrder);

public class BackupRequest
{
    [MaxLength(200)] public string? Label { get; set; }
}

public class AiAssistRequest
{
    [Required, MaxLength(120)] public string Task { get; set; } = "product_description";
    [Required, MaxLength(4000)] public string Prompt { get; set; } = "";
    [MaxLength(120)] public string? Tone { get; set; } = "warm, premium, concise";
}

public record AiAssistResponse(string Content, bool UsedFallback, string Model);
public record AiChatMessage(string Role, string Content);
public class AiChatRequest
{
    [Required, MinLength(1)] public List<AiChatMessage> Messages { get; set; } = [];
}
public record AiChatResponse(string Content, bool UsedFallback, string Model, IReadOnlyList<Guid> RecommendedProductIds);
