using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Application.Dtos.Auth;
using MakeupArt.Application.Dtos.Catalog;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Application.Dtos.Content;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Commerce;
using MakeupArt.Domain.Content;
using MakeupArt.Domain.Identity;

namespace MakeupArt.Application.Mappings;

public static class EntityMappings
{
    public static SiteSettingsDto ToDto(this SiteSetting x) => new()
    {
        Id = x.Id, BusinessName = x.BusinessName, Tagline = x.Tagline, LogoUrl = x.LogoUrl, FaviconUrl = x.FaviconUrl,
        Email = x.Email, Phone = x.Phone, WhatsApp = x.WhatsApp, AddressLine1 = x.AddressLine1, AddressLine2 = x.AddressLine2,
        City = x.City, State = x.State, PostalCode = x.PostalCode, Country = x.Country, MapLatitude = x.MapLatitude,
        MapLongitude = x.MapLongitude, MapZoom = x.MapZoom, GoogleMapsEmbedUrl = x.GoogleMapsEmbedUrl, BusinessHours = x.BusinessHours,
        Currency = x.Currency, PrimaryColor = x.PrimaryColor, SecondaryColor = x.SecondaryColor, AccentColor = x.AccentColor,
        SurfaceColor = x.SurfaceColor, AnnouncementText = x.AnnouncementText, SeoTitle = x.SeoTitle, SeoDescription = x.SeoDescription,
        MusicEnabled = x.MusicEnabled, DefaultMusicTrackId = x.DefaultMusicTrackId, MusicVolume = x.MusicVolume,
        GstEnabled = x.GstEnabled, PricesIncludeTax = x.PricesIncludeTax, GstNumber = x.GstNumber, BusinessState = x.BusinessState,
        InstagramUrl = x.InstagramUrl, FacebookUrl = x.FacebookUrl, YoutubeUrl = x.YoutubeUrl, PinterestUrl = x.PinterestUrl,
        TiktokUrl = x.TiktokUrl, XUrl = x.XUrl, MaintenanceMode = x.MaintenanceMode, SupportEmail = x.SupportEmail,
        SupportPhone = x.SupportPhone, HeroBadge = x.HeroBadge, FooterNote = x.FooterNote
    };

    public static SocialLinkDto ToDto(this SocialLink x) => new(x.Id, x.Platform, x.Url, x.Icon, x.SortOrder, x.IsActive);
    public static MusicTrackDto ToDto(this MusicTrack x) => new(x.Id, x.Title, x.Artist, x.Url, x.DurationSeconds, x.IsDefault, x.SortOrder, x.Credit, x.IsActive);
    public static HeroSlideDto ToDto(this HeroSlide x) => new(x.Id, x.Eyebrow, x.Title, x.Subtitle, x.ModelImageUrl, x.BackgroundImageUrl, x.CtaLabel, x.CtaUrl, x.SecondaryCtaLabel, x.SecondaryCtaUrl, x.AccentColor, x.SecondaryColor, x.EffectJson, x.SortOrder, x.IsActive);
    public static PageSummaryDto ToSummaryDto(this Page x) => new(x.Id, x.Key, x.Title, x.Subtitle, x.IsPublished, x.SortOrder);
    public static PageDto ToDto(this Page x) => new(x.Id, x.Key, x.Title, x.Subtitle, x.SeoTitle, x.SeoDescription, x.IsPublished, x.IsActive, x.Sections.OrderBy(s => s.SortOrder).Select(s => s.ToDto()).ToList());
    public static ContentSectionDto ToDto(this ContentSection x) => new(x.Id, x.Key, x.SectionType, x.Eyebrow, x.Heading, x.Subheading, x.BodyHtml, x.MediaUrl, x.SecondaryMediaUrl, x.CtaLabel, x.CtaUrl, x.ConfigJson, x.SortOrder, x.IsActive);
    public static BannerDto ToDto(this Banner x) => new(x.Id, x.Title, x.Subtitle, x.ImageUrl, x.LinkUrl, x.Placement, x.StartsAtUtc, x.EndsAtUtc, x.SortOrder, x.IsActive);
    public static TestimonialDto ToDto(this Testimonial x) => new(x.Id, x.CustomerName, x.Role, x.Quote, x.Rating, x.ImageUrl, x.VideoUrl, x.IsFeatured, x.Status.ToString(), x.SortOrder, x.CreatedAtUtc, x.IsActive);
    public static FaqDto ToDto(this Faq x) => new(x.Id, x.Question, x.Answer, x.Category, x.SortOrder, x.IsActive);
    public static PortfolioItemDto ToDto(this PortfolioItem x) => new(x.Id, x.Title, x.Category, x.Description, x.ImageUrl, x.VideoUrl, x.TagsCsv, x.IsFeatured, x.SortOrder, x.IsActive);
    public static VlogDto ToDto(this Vlog x) => new(x.Id, x.Title, x.Slug, x.Description, x.Type.ToString(), x.VideoUrl, x.ThumbnailUrl, x.DurationSeconds, x.InfluencerName, x.PublishedAtUtc, x.IsFeatured, x.ViewCount, x.SortOrder, x.IsActive);
    public static MediaAssetDto ToDto(this MediaAsset x) => new(x.Id, x.FileName, x.OriginalFileName, x.Url, x.MediaType.ToString(), x.ContentType, x.SizeBytes, x.Width, x.Height, x.DurationSeconds, x.AltText, x.Credit, x.Folder, x.IsActive, x.CreatedAtUtc);

    public static CategoryDto ToDto(this Category x) => new(x.Id, x.Name, x.Slug, x.Description, x.ImageUrl, x.ParentId, x.SortOrder, x.IsActive);
    public static ProductDto ToDto(this Product x) => new(x.Id, x.Name, x.Slug, x.Sku, x.ShortDescription, x.Description, x.CategoryId, x.Category?.Name, x.Brand, x.Price, x.CompareAtPrice, x.StockQuantity, x.IsFeatured, x.IsPromoted, x.IsNewArrival, x.RatingAverage, x.RatingCount, x.ImageUrl, x.TagsCsv, x.HsnCode, x.SortOrder, x.IsActive);
    public static ProductDetailDto ToDetailDto(this Product x) => new(x.Id, x.Name, x.Slug, x.Sku, x.ShortDescription, x.Description, x.CategoryId, x.Category?.Name, x.Brand, x.Price, x.CompareAtPrice, x.CostPrice, x.StockQuantity, x.LowStockThreshold, x.IsFeatured, x.IsPromoted, x.IsNewArrival, x.RatingAverage, x.RatingCount, x.ImageUrl, x.TagsCsv, x.HsnCode, x.SeoTitle, x.SeoDescription, x.WeightGrams, x.TaxRateId, x.IsActive, x.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.SortOrder)).ToList(), x.Variants.OrderBy(v => v.SortOrder).Select(v => new ProductVariantDto(v.Id, v.Name, v.Sku, v.PriceOverride, v.StockQuantity, v.ImageUrl, v.SortOrder)).ToList(), x.Reviews.Where(r => r.Status == Domain.Common.ReviewStatus.Approved).OrderByDescending(r => r.CreatedAtUtc).Select(r => r.ToDto(x.Name)).ToList());
    public static ReviewDto ToDto(this ProductReview x, string productName) => new(x.Id, x.ProductId, productName, x.User?.DisplayName ?? "Makeup Art client", x.Rating, x.Title, x.Body, x.IsVerifiedPurchase, x.Status.ToString(), x.AdminResponse, x.CreatedAtUtc);
    public static ServiceDto ToDto(this Service x) => new(x.Id, x.Name, x.Slug, x.ShortDescription, x.Description, x.CategoryId, x.Category?.Name, x.Price, x.CompareAtPrice, x.DurationMinutes, x.ImageUrl, x.IsFeatured, x.HsnCode, x.SortOrder, x.IsActive);
    public static ServiceCategoryDto ToDto(this ServiceCategory x) => new(x.Id, x.Name, x.Slug, x.Description, x.ImageUrl, x.SortOrder, x.IsActive);
    public static ServicePackageDto ToDto(this ServicePackage x) => new(x.Id, x.Name, x.Slug, x.ShortDescription, x.Description, x.Price, x.CompareAtPrice, x.DurationMinutes, x.ImageUrl, x.Occasion, x.IsFeatured, x.HsnCode, x.SortOrder, x.IsActive, x.Items.OrderBy(i => i.SortOrder).Select(i => new PackageItemDto(i.Id, i.ServiceId, i.Service?.Name ?? "", i.Quantity, i.SortOrder)).ToList());
    public static ComboDto ToDto(this ProductCombo x) => new(x.Id, x.Name, x.Slug, x.Description, x.Price, x.CompareAtPrice, x.ImageUrl, x.IsFeatured, x.SortOrder, x.IsActive, x.Items.OrderBy(i => i.SortOrder).Select(i => new ComboItemDto(i.Id, i.ProductId, i.Product?.Name, i.ServiceId, i.Service?.Name, i.PackageId, i.Package?.Name, i.Quantity, i.SortOrder)).ToList());
    public static OfferDto ToDto(this Offer x) => new(x.Id, x.Title, x.Subtitle, x.Description, x.Code, x.DiscountType.ToString(), x.DiscountValue, x.ImageUrl, x.AppliesTo.ToString(), x.StartsAtUtc, x.EndsAtUtc, x.IsFeatured, x.SortOrder, x.IsActive);
    public static CouponDto ToDto(this Coupon x) => new(x.Id, x.Code, x.Description, x.DiscountType.ToString(), x.DiscountValue, x.MinimumOrderAmount, x.MaximumDiscountAmount, x.Scope.ToString(), x.StartsAtUtc, x.EndsAtUtc, x.UsageLimit, x.PerUserLimit, x.UsedCount, x.IsActive);
    public static TaxRateDto ToDto(this TaxRate x) => new(x.Id, x.Name, x.RatePercent, x.Country, x.State, x.HsnCode, x.Description, x.IsDefault, x.IsActive);
    public static ShippingMethodDto ToDto(this ShippingMethod x) => new(x.Id, x.Name, x.Description, x.Price, x.FreeAboveAmount, x.EstimatedDays, x.IsDefault, x.SortOrder, x.IsActive);

    public static UserDto ToDto(this ApplicationUser x, IReadOnlyList<string> roles) => new(x.Id, x.Email ?? "", x.FirstName, x.LastName, x.DisplayName, x.PhoneNumber, x.ProfileImageUrl, roles, x.TwoFactorEnabled, x.IsActive, x.CreatedAtUtc, x.MarketingOptIn);
    public static AddressDto ToDto(this CustomerAddress x) => new(x.Id, x.Label, x.FullName, x.Phone, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.Country, x.IsDefaultShipping, x.IsDefaultBilling);
    public static OrderItemDto ToDto(this OrderItem x) => new(x.Id, x.ItemType, x.ItemId, x.Name, x.Sku, x.ImageUrl, x.UnitPrice, x.Quantity, x.TaxRatePercent, x.TaxAmount, x.LineTotal, x.VariantName);
    public static PaymentDto ToDto(this Payment x) => new(x.Id, x.Provider.ToString(), x.ProviderOrderId, x.ProviderPaymentId, x.Amount, x.Currency, x.Status.ToString(), x.Method, x.PaidAtUtc, x.CreatedAtUtc);
    public static OrderSummaryDto ToSummaryDto(this Order x) => new(x.Id, x.OrderNumber, x.CustomerName, x.CustomerEmail, x.Status, x.PaymentStatus, x.TotalAmount, x.Currency, x.Items.Count, x.CreatedAtUtc, x.PaidAtUtc);
    public static OrderDto ToDto(this Order x) => new(x.Id, x.OrderNumber, x.CustomerName, x.CustomerEmail, x.CustomerPhone, x.Status, x.PaymentStatus, x.SubTotal, x.DiscountAmount, x.ShippingAmount, x.TaxAmount, x.TotalAmount, x.Currency, x.CouponCode, x.Notes, x.ShippingMethodName, x.TrackingNumber, x.GstNumber, x.PlaceOfSupply, x.IsGstInvoice, x.PlacedAtUtc, x.PaidAtUtc, x.CreatedAtUtc, x.ShippingAddressJson, x.BillingAddressJson, x.Items.Select(i => i.ToDto()).ToList(), x.Payments.Select(p => p.ToDto()).ToList(), x.Invoice?.InvoiceNumber, x.StatusHistory.OrderBy(h => h.ChangedAtUtc).Select(h => new OrderStatusEventDto(h.Status, h.Note, h.ChangedAtUtc)).ToList());
    public static BookingDto ToDto(this BookingRequest x) => new(x.Id, x.BookingNumber, x.FullName, x.Email, x.Phone, x.Service?.Name, x.Package?.Name, x.PreferredDate, x.PreferredTime, x.Occasion, x.Notes, x.Status.ToString(), x.AdminNotes, x.CreatedAtUtc);
    public static SupportMessageDto ToDto(this SupportMessage x) => new(x.Id, x.SenderName, x.SenderType.ToString(), x.Body, x.AttachmentUrl, x.IsInternal, x.CreatedAtUtc);
    public static SupportTicketSummaryDto ToSummaryDto(this SupportTicket x) => new(x.Id, x.TicketNumber, x.Subject, x.Category, x.Priority.ToString(), x.Status.ToString(), x.Messages.Count, x.LastMessageAtUtc, x.CreatedAtUtc);
    public static SupportTicketDto ToDto(this SupportTicket x) => new(x.Id, x.TicketNumber, x.UserId, x.User?.DisplayName, x.User?.Email, x.Subject, x.Category, x.Priority.ToString(), x.Status.ToString(), x.LastMessageAtUtc, x.CreatedAtUtc, x.Messages.OrderBy(m => m.CreatedAtUtc).Select(m => m.ToDto()).ToList());
}
