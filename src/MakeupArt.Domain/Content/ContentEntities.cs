using MakeupArt.Domain.Common;

namespace MakeupArt.Domain.Content;

public class SiteSetting : BaseEntity
{
    public string BusinessName { get; set; } = "Makeup Art";
    public string Tagline { get; set; } = "Artistry that feels like you.";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string Email { get; set; } = "hello@makeupart.local";
    public string Phone { get; set; } = "+91 98765 43210";
    public string? WhatsApp { get; set; }
    public string AddressLine1 { get; set; } = "12 Rose Atelier, Indiranagar";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "Bengaluru";
    public string State { get; set; } = "Karnataka";
    public string PostalCode { get; set; } = "560038";
    public string Country { get; set; } = "India";
    public double MapLatitude { get; set; } = 12.9716;
    public double MapLongitude { get; set; } = 77.5946;
    public int MapZoom { get; set; } = 14;
    public string? GoogleMapsEmbedUrl { get; set; }
    public string? BusinessHours { get; set; } = "Mon–Sat · 9:00 AM – 8:00 PM";
    public string Currency { get; set; } = "INR";
    public string PrimaryColor { get; set; } = "#8E4A5B";
    public string SecondaryColor { get; set; } = "#C9A26B";
    public string AccentColor { get; set; } = "#E9C8C1";
    public string SurfaceColor { get; set; } = "#FAF6F1";
    public string? AnnouncementText { get; set; } = "Complimentary skin consult with every bridal booking.";
    public string? SeoTitle { get; set; } = "Makeup Art | Bridal, Editorial & Everyday Glam";
    public string? SeoDescription { get; set; } = "A boutique makeup studio for bridal artistry, editorial looks, lessons and curated beauty essentials.";
    public bool MusicEnabled { get; set; } = true;
    public Guid? DefaultMusicTrackId { get; set; }
    public MusicTrack? DefaultMusicTrack { get; set; }
    public int MusicVolume { get; set; } = 35;
    public bool GstEnabled { get; set; } = true;
    public bool PricesIncludeTax { get; set; }
    public string? GstNumber { get; set; } = "29ABCDE1234F1Z5";
    public string BusinessState { get; set; } = "Karnataka";
    public string? InstagramUrl { get; set; } = "https://instagram.com";
    public string? FacebookUrl { get; set; } = "https://facebook.com";
    public string? YoutubeUrl { get; set; } = "https://youtube.com";
    public string? PinterestUrl { get; set; } = "https://pinterest.com";
    public string? TiktokUrl { get; set; }
    public string? XUrl { get; set; }
    public bool MaintenanceMode { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? HeroBadge { get; set; } = "Bengaluru · Bridal & Editorial Studio";
    public string? FooterNote { get; set; } = "Made with care, colour and a little bit of magic.";
}

public class Page : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
    public List<ContentSection> Sections { get; set; } = [];
}

public class ContentSection : BaseEntity
{
    public Guid PageId { get; set; }
    public Page? Page { get; set; }
    public string Key { get; set; } = string.Empty;
    public string SectionType { get; set; } = "text";
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
}

public class HeroSlide : BaseEntity
{
    public string Eyebrow { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? ModelImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? CtaLabel { get; set; } = "Explore services";
    public string? CtaUrl { get; set; } = "/services";
    public string? SecondaryCtaLabel { get; set; } = "Shop the edit";
    public string? SecondaryCtaUrl { get; set; } = "/products";
    public string AccentColor { get; set; } = "#E9C8C1";
    public string SecondaryColor { get; set; } = "#C9A26B";
    public string? EffectJson { get; set; }
    public int SortOrder { get; set; }
}

public class SocialLink : BaseEntity
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "link";
    public int SortOrder { get; set; }
}

public class MusicTrack : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = "Makeup Art Studio";
    public string Url { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public string? Credit { get; set; }
}

public class Banner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string Placement { get; set; } = "home";
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int SortOrder { get; set; }
}

public class Testimonial : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string Quote { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsFeatured { get; set; }
    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;
    public Guid? UserId { get; set; }
    public int SortOrder { get; set; }
}

public class Faq : BaseEntity
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int SortOrder { get; set; }
}

public class PortfolioItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Bridal";
    public string? Description { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? TagsCsv { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
}

public class Vlog : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VlogType Type { get; set; } = VlogType.Makeup;
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int DurationSeconds { get; set; }
    public string? InfluencerName { get; set; }
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public int SortOrder { get; set; }
}

public class EmailTemplate : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class MediaAsset : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; }
    public string? AltText { get; set; }
    public string? Credit { get; set; }
    public string Folder { get; set; } = "general";
    public bool IsPublic { get; set; } = true;
    public string? Hash { get; set; }
}

public class AnalyticsEvent : BaseEntity
{
    public string EventType { get; set; } = "page_view";
    public string Path { get; set; } = "/";
    public string? SessionId { get; set; }
    public Guid? UserId { get; set; }
    public string? Referrer { get; set; }
    public string? DeviceType { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public class BackupRecord : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public BackupStatus Status { get; set; } = BackupStatus.Started;
    public string? Message { get; set; }
    public string BackupType { get; set; } = "scheduled";
}
