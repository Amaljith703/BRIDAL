using MakeupArt.Application.Dtos.Catalog;
using MakeupArt.Domain.Common;

namespace MakeupArt.Application.Dtos.Content;

public class SiteSettingsDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? WhatsApp { get; set; }
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
    public double MapLatitude { get; set; }
    public double MapLongitude { get; set; }
    public int MapZoom { get; set; }
    public string? GoogleMapsEmbedUrl { get; set; }
    public string? BusinessHours { get; set; }
    public string Currency { get; set; } = "INR";
    public string PrimaryColor { get; set; } = "";
    public string SecondaryColor { get; set; } = "";
    public string AccentColor { get; set; } = "";
    public string SurfaceColor { get; set; } = "";
    public string? AnnouncementText { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool MusicEnabled { get; set; }
    public Guid? DefaultMusicTrackId { get; set; }
    public int MusicVolume { get; set; }
    public bool GstEnabled { get; set; }
    public bool PricesIncludeTax { get; set; }
    public string? GstNumber { get; set; }
    public string BusinessState { get; set; } = "";
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? PinterestUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? XUrl { get; set; }
    public bool MaintenanceMode { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? HeroBadge { get; set; }
    public string? FooterNote { get; set; }
}

public record SocialLinkDto(Guid Id, string Platform, string Url, string Icon, int SortOrder, bool IsActive);
public record MusicTrackDto(Guid Id, string Title, string Artist, string Url, int DurationSeconds, bool IsDefault, int SortOrder, string? Credit, bool IsActive);
public record HeroSlideDto(Guid Id, string Eyebrow, string Title, string Subtitle, string? ModelImageUrl, string? BackgroundImageUrl, string? CtaLabel, string? CtaUrl, string? SecondaryCtaLabel, string? SecondaryCtaUrl, string AccentColor, string SecondaryColor, string? EffectJson, int SortOrder, bool IsActive);
public record PageSummaryDto(Guid Id, string Key, string Title, string? Subtitle, bool IsPublished, int SortOrder);
public record PageDto(Guid Id, string Key, string Title, string? Subtitle, string? SeoTitle, string? SeoDescription, bool IsPublished, bool IsActive, IReadOnlyList<ContentSectionDto> Sections);
public record ContentSectionDto(Guid Id, string Key, string SectionType, string? Eyebrow, string? Heading, string? Subheading, string? BodyHtml, string? MediaUrl, string? SecondaryMediaUrl, string? CtaLabel, string? CtaUrl, string? ConfigJson, int SortOrder, bool IsActive);
public record BannerDto(Guid Id, string Title, string? Subtitle, string ImageUrl, string? LinkUrl, string Placement, DateTime? StartsAtUtc, DateTime? EndsAtUtc, int SortOrder, bool IsActive);
public record TestimonialDto(Guid Id, string CustomerName, string? Role, string Quote, int Rating, string? ImageUrl, string? VideoUrl, bool IsFeatured, string Status, int SortOrder, DateTime CreatedAtUtc, bool IsActive);
public record FaqDto(Guid Id, string Question, string Answer, string Category, int SortOrder, bool IsActive);
public record PortfolioItemDto(Guid Id, string Title, string Category, string? Description, string ImageUrl, string? VideoUrl, string? TagsCsv, bool IsFeatured, int SortOrder, bool IsActive);
public record VlogDto(Guid Id, string Title, string Slug, string? Description, string Type, string? VideoUrl, string? ThumbnailUrl, int DurationSeconds, string? InfluencerName, DateTime PublishedAtUtc, bool IsFeatured, int ViewCount, int SortOrder, bool IsActive);
public record MediaAssetDto(Guid Id, string FileName, string OriginalFileName, string Url, string MediaType, string ContentType, long SizeBytes, int? Width, int? Height, int? DurationSeconds, string? AltText, string? Credit, string Folder, bool IsActive, DateTime CreatedAtUtc);
public record BootstrapDto(
    SiteSettingsDto Settings,
    IReadOnlyList<SocialLinkDto> SocialLinks,
    IReadOnlyList<MusicTrackDto> Music,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<HeroSlideDto> HeroSlides,
    IReadOnlyList<ServiceDto> FeaturedServices,
    IReadOnlyList<ProductDto> FeaturedProducts,
    IReadOnlyList<ServicePackageDto> FeaturedPackages,
    IReadOnlyList<TestimonialDto> Testimonials,
    IReadOnlyList<FaqDto> Faqs,
    IReadOnlyList<PortfolioItemDto> Portfolio,
    IReadOnlyList<VlogDto> Vlogs,
    IReadOnlyList<OfferDto> Offers,
    IReadOnlyList<BannerDto> Banners);
