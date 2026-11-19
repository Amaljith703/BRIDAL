using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos;
using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Application.Dtos.Content;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Content;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers.Admin;

[Authorize(Policy = "Staff")]
[Route("api/admin/content")]
public class AdminContentController(AppDbContext db, IHtmlSanitizerService sanitizer) : BaseApiController
{
    [HttpGet("settings")]
    public async Task<ActionResult<SiteSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                       ?? throw ApiException.NotFound("Site settings");
        return settings.ToDto();
    }

    [HttpPut("settings")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<SiteSettingsDto>> UpdateSettings(SiteSettingsDto request, CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.FirstOrDefaultAsync(cancellationToken)
                       ?? throw ApiException.NotFound("Site settings");
        settings.BusinessName = Required(request.BusinessName, 200, "Business name");
        settings.Tagline = Required(request.Tagline, 250, "Tagline");
        settings.LogoUrl = request.LogoUrl;
        settings.FaviconUrl = request.FaviconUrl;
        settings.Email = Required(request.Email, 256, "Email");
        settings.Phone = request.Phone;
        settings.WhatsApp = request.WhatsApp;
        settings.AddressLine1 = request.AddressLine1;
        settings.AddressLine2 = request.AddressLine2;
        settings.City = request.City;
        settings.State = request.State;
        settings.PostalCode = request.PostalCode;
        settings.Country = request.Country;
        settings.MapLatitude = request.MapLatitude;
        settings.MapLongitude = request.MapLongitude;
        settings.MapZoom = Math.Clamp(request.MapZoom, 2, 20);
        settings.GoogleMapsEmbedUrl = request.GoogleMapsEmbedUrl;
        settings.BusinessHours = request.BusinessHours;
        settings.Currency = request.Currency;
        settings.PrimaryColor = request.PrimaryColor;
        settings.SecondaryColor = request.SecondaryColor;
        settings.AccentColor = request.AccentColor;
        settings.SurfaceColor = request.SurfaceColor;
        settings.AnnouncementText = request.AnnouncementText;
        settings.SeoTitle = request.SeoTitle;
        settings.SeoDescription = request.SeoDescription;
        settings.MusicEnabled = request.MusicEnabled;
        settings.DefaultMusicTrackId = request.DefaultMusicTrackId;
        settings.MusicVolume = Math.Clamp(request.MusicVolume, 0, 100);
        settings.GstEnabled = request.GstEnabled;
        settings.PricesIncludeTax = request.PricesIncludeTax;
        settings.GstNumber = request.GstNumber;
        settings.BusinessState = request.BusinessState;
        settings.InstagramUrl = request.InstagramUrl;
        settings.FacebookUrl = request.FacebookUrl;
        settings.YoutubeUrl = request.YoutubeUrl;
        settings.PinterestUrl = request.PinterestUrl;
        settings.TiktokUrl = request.TiktokUrl;
        settings.XUrl = request.XUrl;
        settings.MaintenanceMode = request.MaintenanceMode;
        settings.SupportEmail = request.SupportEmail;
        settings.SupportPhone = request.SupportPhone;
        settings.HeroBadge = request.HeroBadge;
        settings.FooterNote = request.FooterNote;
        await db.SaveChangesAsync(cancellationToken);
        return settings.ToDto();
    }

    // ── Hero slides ────────────────────────────────────────────────────────
    [HttpGet("hero-slides")]
    public async Task<ActionResult<IReadOnlyList<HeroSlideDto>>> GetHeroSlides(CancellationToken cancellationToken)
    {
        var slides = await db.HeroSlides.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return slides.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("hero-slides")]
    public async Task<ActionResult<HeroSlideDto>> CreateHeroSlide(HeroSlideRequest request, CancellationToken cancellationToken)
    {
        var slide = new HeroSlide();
        Apply(slide, request);
        db.HeroSlides.Add(slide);
        await db.SaveChangesAsync(cancellationToken);
        return slide.ToDto();
    }

    [HttpPut("hero-slides/{id:guid}")]
    public async Task<ActionResult<HeroSlideDto>> UpdateHeroSlide(Guid id, HeroSlideRequest request, CancellationToken cancellationToken)
    {
        var slide = await db.HeroSlides.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Hero slide");
        Apply(slide, request);
        await db.SaveChangesAsync(cancellationToken);
        return slide.ToDto();
    }

    [HttpDelete("hero-slides/{id:guid}")]
    public async Task<IActionResult> DeleteHeroSlide(Guid id, CancellationToken cancellationToken)
    {
        var slide = await db.HeroSlides.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Hero slide");
        db.HeroSlides.Remove(slide);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("hero-slides/reorder")]
    public async Task<IActionResult> ReorderHeroSlides(ReorderRequest request, CancellationToken cancellationToken)
    {
        var ids = request.Items.Select(x => x.Id).ToList();
        var slides = await db.HeroSlides.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var slide in slides)
            slide.SortOrder = request.Items.First(x => x.Id == slide.Id).SortOrder;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Social links ───────────────────────────────────────────────────────
    [HttpGet("social-links")]
    public async Task<ActionResult<IReadOnlyList<SocialLinkDto>>> GetSocialLinks(CancellationToken cancellationToken)
    {
        var links = await db.SocialLinks.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return links.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("social-links")]
    public async Task<ActionResult<SocialLinkDto>> CreateSocialLink(SocialLinkRequest request, CancellationToken cancellationToken)
    {
        var link = new SocialLink();
        Apply(link, request);
        db.SocialLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return link.ToDto();
    }

    [HttpPut("social-links/{id:guid}")]
    public async Task<ActionResult<SocialLinkDto>> UpdateSocialLink(Guid id, SocialLinkRequest request, CancellationToken cancellationToken)
    {
        var link = await db.SocialLinks.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Social link");
        Apply(link, request);
        await db.SaveChangesAsync(cancellationToken);
        return link.ToDto();
    }

    [HttpDelete("social-links/{id:guid}")]
    public async Task<IActionResult> DeleteSocialLink(Guid id, CancellationToken cancellationToken)
    {
        var link = await db.SocialLinks.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Social link");
        db.SocialLinks.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Music ──────────────────────────────────────────────────────────────
    [HttpGet("music")]
    public async Task<ActionResult<IReadOnlyList<MusicTrackDto>>> GetMusic(CancellationToken cancellationToken)
    {
        var tracks = await db.MusicTracks.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return tracks.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("music")]
    public async Task<ActionResult<MusicTrackDto>> CreateMusic(MusicTrackRequest request, CancellationToken cancellationToken)
    {
        var track = new MusicTrack();
        await Apply(track, request, cancellationToken);
        db.MusicTracks.Add(track);
        await db.SaveChangesAsync(cancellationToken);
        return track.ToDto();
    }

    [HttpPut("music/{id:guid}")]
    public async Task<ActionResult<MusicTrackDto>> UpdateMusic(Guid id, MusicTrackRequest request, CancellationToken cancellationToken)
    {
        var track = await db.MusicTracks.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Music track");
        await Apply(track, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return track.ToDto();
    }

    [HttpDelete("music/{id:guid}")]
    public async Task<IActionResult> DeleteMusic(Guid id, CancellationToken cancellationToken)
    {
        var track = await db.MusicTracks.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Music track");
        var settings = await db.SiteSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings?.DefaultMusicTrackId == id) settings.DefaultMusicTrackId = null;
        db.MusicTracks.Remove(track);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Banners ────────────────────────────────────────────────────────────
    [HttpGet("banners")]
    public async Task<ActionResult<IReadOnlyList<BannerDto>>> GetBanners(CancellationToken cancellationToken)
    {
        var banners = await db.Banners.AsNoTracking().OrderBy(x => x.Placement).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return banners.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("banners")]
    public async Task<ActionResult<BannerDto>> CreateBanner(BannerRequest request, CancellationToken cancellationToken)
    {
        var banner = new Banner();
        Apply(banner, request);
        db.Banners.Add(banner);
        await db.SaveChangesAsync(cancellationToken);
        return banner.ToDto();
    }

    [HttpPut("banners/{id:guid}")]
    public async Task<ActionResult<BannerDto>> UpdateBanner(Guid id, BannerRequest request, CancellationToken cancellationToken)
    {
        var banner = await db.Banners.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Banner");
        Apply(banner, request);
        await db.SaveChangesAsync(cancellationToken);
        return banner.ToDto();
    }

    [HttpDelete("banners/{id:guid}")]
    public async Task<IActionResult> DeleteBanner(Guid id, CancellationToken cancellationToken)
    {
        var banner = await db.Banners.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Banner");
        db.Banners.Remove(banner);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Testimonials ───────────────────────────────────────────────────────
    [HttpGet("testimonials")]
    public async Task<ActionResult<PagedResult<TestimonialDto>>> GetTestimonials([FromQuery] PagedRequest request, [FromQuery] TestimonialStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Testimonials.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.CustomerName.Contains(term) || x.Quote.Contains(term));
        }
        query = query.OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<TestimonialDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("testimonials")]
    public async Task<ActionResult<TestimonialDto>> CreateTestimonial(TestimonialRequest request, CancellationToken cancellationToken)
    {
        var testimonial = new Testimonial();
        Apply(testimonial, request);
        db.Testimonials.Add(testimonial);
        await db.SaveChangesAsync(cancellationToken);
        return testimonial.ToDto();
    }

    [HttpPut("testimonials/{id:guid}")]
    public async Task<ActionResult<TestimonialDto>> UpdateTestimonial(Guid id, TestimonialRequest request, CancellationToken cancellationToken)
    {
        var testimonial = await db.Testimonials.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Testimonial");
        Apply(testimonial, request);
        await db.SaveChangesAsync(cancellationToken);
        return testimonial.ToDto();
    }

    [HttpDelete("testimonials/{id:guid}")]
    public async Task<IActionResult> DeleteTestimonial(Guid id, CancellationToken cancellationToken)
    {
        var testimonial = await db.Testimonials.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Testimonial");
        db.Testimonials.Remove(testimonial);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── FAQs ───────────────────────────────────────────────────────────────
    [HttpGet("faqs")]
    public async Task<ActionResult<IReadOnlyList<FaqDto>>> GetFaqs(CancellationToken cancellationToken)
    {
        var faqs = await db.Faqs.AsNoTracking().OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return faqs.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("faqs")]
    public async Task<ActionResult<FaqDto>> CreateFaq(FaqRequest request, CancellationToken cancellationToken)
    {
        var faq = new Faq();
        Apply(faq, request);
        db.Faqs.Add(faq);
        await db.SaveChangesAsync(cancellationToken);
        return faq.ToDto();
    }

    [HttpPut("faqs/{id:guid}")]
    public async Task<ActionResult<FaqDto>> UpdateFaq(Guid id, FaqRequest request, CancellationToken cancellationToken)
    {
        var faq = await db.Faqs.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("FAQ");
        Apply(faq, request);
        await db.SaveChangesAsync(cancellationToken);
        return faq.ToDto();
    }

    [HttpDelete("faqs/{id:guid}")]
    public async Task<IActionResult> DeleteFaq(Guid id, CancellationToken cancellationToken)
    {
        var faq = await db.Faqs.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("FAQ");
        db.Faqs.Remove(faq);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Portfolio ──────────────────────────────────────────────────────────
    [HttpGet("portfolio")]
    public async Task<ActionResult<PagedResult<PortfolioItemDto>>> GetPortfolio([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.PortfolioItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Category.Contains(term));
        }
        query = query.OrderBy(x => x.Category).ThenBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<PortfolioItemDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("portfolio")]
    public async Task<ActionResult<PortfolioItemDto>> CreatePortfolio(PortfolioItemRequest request, CancellationToken cancellationToken)
    {
        var item = new PortfolioItem();
        Apply(item, request);
        db.PortfolioItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.ToDto();
    }

    [HttpPut("portfolio/{id:guid}")]
    public async Task<ActionResult<PortfolioItemDto>> UpdatePortfolio(Guid id, PortfolioItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.PortfolioItems.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Portfolio item");
        Apply(item, request);
        await db.SaveChangesAsync(cancellationToken);
        return item.ToDto();
    }

    [HttpDelete("portfolio/{id:guid}")]
    public async Task<IActionResult> DeletePortfolio(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.PortfolioItems.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Portfolio item");
        db.PortfolioItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Vlogs ──────────────────────────────────────────────────────────────
    [HttpGet("vlogs")]
    public async Task<ActionResult<PagedResult<VlogDto>>> GetVlogs([FromQuery] PagedRequest request, [FromQuery] VlogType? type, CancellationToken cancellationToken)
    {
        var query = db.Vlogs.AsNoTracking().AsQueryable();
        if (type is not null) query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || (x.InfluencerName != null && x.InfluencerName.Contains(term)));
        }
        query = query.OrderByDescending(x => x.PublishedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<VlogDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("vlogs")]
    public async Task<ActionResult<VlogDto>> CreateVlog(VlogRequest request, CancellationToken cancellationToken)
    {
        var vlog = new Vlog();
        await Apply(vlog, request, cancellationToken);
        db.Vlogs.Add(vlog);
        await db.SaveChangesAsync(cancellationToken);
        return vlog.ToDto();
    }

    [HttpPut("vlogs/{id:guid}")]
    public async Task<ActionResult<VlogDto>> UpdateVlog(Guid id, VlogRequest request, CancellationToken cancellationToken)
    {
        var vlog = await db.Vlogs.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Vlog");
        await Apply(vlog, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return vlog.ToDto();
    }

    [HttpDelete("vlogs/{id:guid}")]
    public async Task<IActionResult> DeleteVlog(Guid id, CancellationToken cancellationToken)
    {
        var vlog = await db.Vlogs.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Vlog");
        db.Vlogs.Remove(vlog);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Pages & sections ───────────────────────────────────────────────────
    [HttpGet("pages")]
    public async Task<ActionResult<IReadOnlyList<PageDto>>> GetPages(CancellationToken cancellationToken)
    {
        var pages = await db.Pages.AsNoTracking().Include(x => x.Sections).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return pages.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("pages/{id:guid}")]
    public async Task<ActionResult<PageDto>> GetPage(Guid id, CancellationToken cancellationToken)
    {
        var page = await db.Pages.AsNoTracking().Include(x => x.Sections).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                   ?? throw ApiException.NotFound("Page");
        return page.ToDto();
    }

    [HttpPost("pages")]
    public async Task<ActionResult<PageDto>> CreatePage(PageRequest request, CancellationToken cancellationToken)
    {
        if (await db.Pages.AnyAsync(x => x.Key == request.Key, cancellationToken))
            throw ApiException.Conflict("A page with that key already exists.");
        var page = new Page();
        Apply(page, request);
        db.Pages.Add(page);
        await db.SaveChangesAsync(cancellationToken);
        return page.ToDto();
    }

    [HttpPut("pages/{id:guid}")]
    public async Task<ActionResult<PageDto>> UpdatePage(Guid id, PageRequest request, CancellationToken cancellationToken)
    {
        var page = await db.Pages.Include(x => x.Sections).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                   ?? throw ApiException.NotFound("Page");
        if (await db.Pages.AnyAsync(x => x.Key == request.Key && x.Id != id, cancellationToken))
            throw ApiException.Conflict("A page with that key already exists.");
        page.Key = request.Key;
        page.Title = request.Title;
        page.Subtitle = request.Subtitle;
        page.SeoTitle = request.SeoTitle;
        page.SeoDescription = request.SeoDescription;
        page.IsPublished = request.IsPublished;
        page.SortOrder = request.SortOrder;
        page.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return page.ToDto();
    }

    [HttpDelete("pages/{id:guid}")]
    public async Task<IActionResult> DeletePage(Guid id, CancellationToken cancellationToken)
    {
        var page = await db.Pages.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Page");
        db.Pages.Remove(page);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("pages/{pageId:guid}/sections")]
    public async Task<ActionResult<ContentSectionDto>> CreateSection(Guid pageId, ContentSectionRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Pages.AnyAsync(x => x.Id == pageId, cancellationToken)) throw ApiException.NotFound("Page");
        var section = new ContentSection { PageId = pageId };
        Apply(section, request);
        db.ContentSections.Add(section);
        await db.SaveChangesAsync(cancellationToken);
        return section.ToDto();
    }

    [HttpPut("sections/{id:guid}")]
    public async Task<ActionResult<ContentSectionDto>> UpdateSection(Guid id, ContentSectionRequest request, CancellationToken cancellationToken)
    {
        var section = await db.ContentSections.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Section");
        Apply(section, request);
        await db.SaveChangesAsync(cancellationToken);
        return section.ToDto();
    }

    [HttpDelete("sections/{id:guid}")]
    public async Task<IActionResult> DeleteSection(Guid id, CancellationToken cancellationToken)
    {
        var section = await db.ContentSections.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Section");
        db.ContentSections.Remove(section);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Email templates ────────────────────────────────────────────────────
    [HttpGet("email-templates")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetEmailTemplates(CancellationToken cancellationToken)
    {
        var templates = await db.EmailTemplates.AsNoTracking().OrderBy(x => x.Key)
            .Select(x => new { x.Id, x.Key, x.Subject, x.BodyHtml, x.Description, x.IsActive })
            .ToListAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpPut("email-templates/{id:guid}")]
    public async Task<IActionResult> UpdateEmailTemplate(Guid id, [FromBody] EmailTemplateUpdate request, CancellationToken cancellationToken)
    {
        var template = await db.EmailTemplates.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Email template");
        template.Subject = Required(request.Subject, 300, "Subject");
        template.BodyHtml = sanitizer.Sanitize(request.BodyHtml);
        template.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw ApiException.Validation($"{field} is required.");
        return value.Length > max ? value[..max] : value.Trim();
    }

    private static void Apply(HeroSlide slide, HeroSlideRequest r)
    {
        slide.Eyebrow = r.Eyebrow;
        slide.Title = r.Title;
        slide.Subtitle = r.Subtitle;
        slide.ModelImageUrl = r.ModelImageUrl;
        slide.BackgroundImageUrl = r.BackgroundImageUrl;
        slide.CtaLabel = r.CtaLabel;
        slide.CtaUrl = r.CtaUrl;
        slide.SecondaryCtaLabel = r.SecondaryCtaLabel;
        slide.SecondaryCtaUrl = r.SecondaryCtaUrl;
        slide.AccentColor = r.AccentColor;
        slide.SecondaryColor = r.SecondaryColor;
        slide.EffectJson = r.EffectJson;
        slide.SortOrder = r.SortOrder;
        slide.IsActive = r.IsActive;
    }

    private static void Apply(SocialLink link, SocialLinkRequest r)
    {
        link.Platform = r.Platform;
        link.Url = r.Url;
        link.Icon = r.Icon;
        link.SortOrder = r.SortOrder;
        link.IsActive = r.IsActive;
    }

    private async Task Apply(MusicTrack track, MusicTrackRequest r, CancellationToken cancellationToken)
    {
        if (r.IsDefault)
        {
            var others = await db.MusicTracks.Where(x => x.Id != track.Id && x.IsDefault).ToListAsync(cancellationToken);
            foreach (var other in others) other.IsDefault = false;
        }
        track.Title = r.Title;
        track.Artist = r.Artist;
        track.Url = r.Url;
        track.DurationSeconds = r.DurationSeconds;
        track.IsDefault = r.IsDefault;
        track.SortOrder = r.SortOrder;
        track.Credit = r.Credit;
        track.IsActive = r.IsActive;
    }

    private static void Apply(Banner banner, BannerRequest r)
    {
        banner.Title = r.Title;
        banner.Subtitle = r.Subtitle;
        banner.ImageUrl = r.ImageUrl;
        banner.LinkUrl = r.LinkUrl;
        banner.Placement = r.Placement;
        banner.StartsAtUtc = r.StartsAtUtc;
        banner.EndsAtUtc = r.EndsAtUtc;
        banner.SortOrder = r.SortOrder;
        banner.IsActive = r.IsActive;
    }

    private static void Apply(Testimonial testimonial, TestimonialRequest r)
    {
        testimonial.CustomerName = r.CustomerName;
        testimonial.Role = r.Role;
        testimonial.Quote = r.Quote;
        testimonial.Rating = Math.Clamp(r.Rating, 1, 5);
        testimonial.ImageUrl = r.ImageUrl;
        testimonial.VideoUrl = r.VideoUrl;
        testimonial.IsFeatured = r.IsFeatured;
        testimonial.Status = r.Status;
        testimonial.SortOrder = r.SortOrder;
        testimonial.IsActive = r.IsActive;
    }

    private static void Apply(Faq faq, FaqRequest r)
    {
        faq.Question = r.Question;
        faq.Answer = r.Answer;
        faq.Category = r.Category;
        faq.SortOrder = r.SortOrder;
        faq.IsActive = r.IsActive;
    }

    private static void Apply(PortfolioItem item, PortfolioItemRequest r)
    {
        item.Title = r.Title;
        item.Category = r.Category;
        item.Description = r.Description;
        item.ImageUrl = r.ImageUrl;
        item.VideoUrl = r.VideoUrl;
        item.TagsCsv = r.TagsCsv;
        item.IsFeatured = r.IsFeatured;
        item.SortOrder = r.SortOrder;
        item.IsActive = r.IsActive;
    }

    private async Task Apply(Vlog vlog, VlogRequest r, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(r.Slug) ? Slugger.Create(r.Title) : Slugger.Create(r.Slug);
        var existing = await db.Vlogs.IgnoreQueryFilters().Where(x => x.Id != vlog.Id).Select(x => x.Slug).ToListAsync(cancellationToken);
        vlog.Title = r.Title;
        vlog.Slug = Slugger.Unique(slug, existing);
        vlog.Description = r.Description;
        vlog.Type = r.Type;
        vlog.VideoUrl = r.VideoUrl;
        vlog.ThumbnailUrl = r.ThumbnailUrl;
        vlog.DurationSeconds = r.DurationSeconds;
        vlog.InfluencerName = r.InfluencerName;
        vlog.PublishedAtUtc = r.PublishedAtUtc;
        vlog.IsFeatured = r.IsFeatured;
        vlog.SortOrder = r.SortOrder;
        vlog.IsActive = r.IsActive;
    }

    private static void Apply(Page page, PageRequest r)
    {
        page.Key = Slugger.Create(r.Key);
        page.Title = r.Title;
        page.Subtitle = r.Subtitle;
        page.SeoTitle = r.SeoTitle;
        page.SeoDescription = r.SeoDescription;
        page.IsPublished = r.IsPublished;
        page.SortOrder = r.SortOrder;
        page.IsActive = r.IsActive;
    }

    private void Apply(ContentSection section, ContentSectionRequest r)
    {
        section.Key = Slugger.Create(r.Key);
        section.SectionType = r.SectionType;
        section.Eyebrow = r.Eyebrow;
        section.Heading = r.Heading;
        section.Subheading = r.Subheading;
        section.BodyHtml = sanitizer.Sanitize(r.BodyHtml);
        section.MediaUrl = r.MediaUrl;
        section.SecondaryMediaUrl = r.SecondaryMediaUrl;
        section.CtaLabel = r.CtaLabel;
        section.CtaUrl = r.CtaUrl;
        section.ConfigJson = r.ConfigJson;
        section.SortOrder = r.SortOrder;
        section.IsActive = r.IsActive;
    }
}

public class EmailTemplateUpdate
{
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
