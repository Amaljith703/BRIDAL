using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos;
using MakeupArt.Application.Dtos.Catalog;
using MakeupArt.Application.Dtos.Content;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Common;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers;

[Route("api/content")]
public class ContentController(AppDbContext db) : BaseApiController
{
    [HttpGet("bootstrap")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<BootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                       ?? throw ApiException.NotFound("Site settings");

        var socials = await db.SocialLinks.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var music = await db.MusicTracks.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var slides = await db.HeroSlides.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var services = await db.Services.AsNoTracking().Where(x => x.IsActive && x.IsFeatured)
            .Include(x => x.Category).OrderBy(x => x.SortOrder).Take(8).ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().Where(x => x.IsActive && x.IsFeatured)
            .Include(x => x.Category).OrderByDescending(x => x.IsPromoted).ThenBy(x => x.SortOrder).Take(8).ToListAsync(cancellationToken);
        var packages = await db.ServicePackages.AsNoTracking().Where(x => x.IsActive && x.IsFeatured)
            .Include(x => x.Items).ThenInclude(x => x.Service)
            .OrderBy(x => x.SortOrder).Take(6).ToListAsync(cancellationToken);
        var testimonials = await db.Testimonials.AsNoTracking()
            .Where(x => x.IsActive && x.Status == TestimonialStatus.Approved && x.IsFeatured)
            .OrderBy(x => x.SortOrder).Take(12).ToListAsync(cancellationToken);
        var faqs = await db.Faqs.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var portfolio = await db.PortfolioItems.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).Take(16).ToListAsync(cancellationToken);
        var vlogs = await db.Vlogs.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).Take(8).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var offers = await db.Offers.AsNoTracking().Where(x => x.IsActive)
            .Where(x => (x.StartsAtUtc == null || x.StartsAtUtc <= now) && (x.EndsAtUtc == null || x.EndsAtUtc >= now))
            .OrderBy(x => x.SortOrder).Take(8).ToListAsync(cancellationToken);
        var banners = await db.Banners.AsNoTracking().Where(x => x.IsActive && x.Placement == "home")
            .Where(x => (x.StartsAtUtc == null || x.StartsAtUtc <= now) && (x.EndsAtUtc == null || x.EndsAtUtc >= now))
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

        return new BootstrapDto(
            settings.ToDto(),
            socials.Select(x => x.ToDto()).ToList(),
            music.Select(x => x.ToDto()).ToList(),
            categories.Select(x => x.ToDto()).ToList(),
            slides.Select(x => x.ToDto()).ToList(),
            services.Select(x => x.ToDto()).ToList(),
            products.Select(x => x.ToDto()).ToList(),
            packages.Select(x => x.ToDto()).ToList(),
            testimonials.Select(x => x.ToDto()).ToList(),
            faqs.Select(x => x.ToDto()).ToList(),
            portfolio.Select(x => x.ToDto()).ToList(),
            vlogs.Select(x => x.ToDto()).ToList(),
            offers.Select(x => x.ToDto()).ToList(),
            banners.Select(x => x.ToDto()).ToList());
    }

    [HttpGet("settings")]
    public async Task<ActionResult<SiteSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                       ?? throw ApiException.NotFound("Site settings");
        return settings.ToDto();
    }

    [HttpGet("hero-slides")]
    public async Task<ActionResult<IReadOnlyList<HeroSlideDto>>> GetHeroSlides(CancellationToken cancellationToken)
    {
        var slides = await db.HeroSlides.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return slides.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("pages")]
    public async Task<ActionResult<IReadOnlyList<PageSummaryDto>>> GetPages(CancellationToken cancellationToken)
    {
        var pages = await db.Pages.AsNoTracking().Where(x => x.IsActive && x.IsPublished)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return pages.Select(x => x.ToSummaryDto()).ToList();
    }

    [HttpGet("pages/{key}")]
    public async Task<ActionResult<PageDto>> GetPage(string key, CancellationToken cancellationToken)
    {
        var page = await db.Pages.AsNoTracking().Include(x => x.Sections)
            .FirstOrDefaultAsync(x => x.Key == key && x.IsActive && x.IsPublished, cancellationToken)
            ?? throw ApiException.NotFound("Page");
        page.Sections = page.Sections.Where(x => x.IsActive).ToList();
        return page.ToDto();
    }

    [HttpGet("music")]
    public async Task<ActionResult<IReadOnlyList<MusicTrackDto>>> GetMusic(CancellationToken cancellationToken)
    {
        var tracks = await db.MusicTracks.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return tracks.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("social-links")]
    public async Task<ActionResult<IReadOnlyList<SocialLinkDto>>> GetSocialLinks(CancellationToken cancellationToken)
    {
        var links = await db.SocialLinks.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return links.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("testimonials")]
    public async Task<ActionResult<PagedResult<TestimonialDto>>> GetTestimonials(
        [FromQuery] PagedRequest request, [FromQuery] string? category, CancellationToken cancellationToken)
    {
        var query = db.Testimonials.AsNoTracking().Where(x => x.IsActive && x.Status == TestimonialStatus.Approved);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.CustomerName.Contains(term) || x.Quote.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.Role != null && x.Role.Contains(category));
        query = query.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<TestimonialDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("faqs")]
    public async Task<ActionResult<PagedResult<FaqDto>>> GetFaqs([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.Faqs.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Question.Contains(term) || x.Answer.Contains(term) || x.Category.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(request.SortBy) && request.SortBy.Equals("category", StringComparison.OrdinalIgnoreCase))
            query = query.OrderBy(x => x.Category).ThenBy(x => x.SortOrder);
        else
            query = query.OrderBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<FaqDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("portfolio")]
    public async Task<ActionResult<PagedResult<PortfolioItemDto>>> GetPortfolio(
        [FromQuery] PagedRequest request, [FromQuery] string? category, CancellationToken cancellationToken)
    {
        var query = db.PortfolioItems.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || (x.Description != null && x.Description.Contains(term)) || (x.TagsCsv != null && x.TagsCsv.Contains(term)));
        }
        query = query.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<PortfolioItemDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("vlogs")]
    public async Task<ActionResult<PagedResult<VlogDto>>> GetVlogs(
        [FromQuery] PagedRequest request, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        var query = db.Vlogs.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<VlogType>(type, true, out var vlogType))
            query = query.Where(x => x.Type == vlogType);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || (x.Description != null && x.Description.Contains(term)) || (x.InfluencerName != null && x.InfluencerName.Contains(term)));
        }
        query = query.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder).ThenByDescending(x => x.PublishedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<VlogDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("vlogs/{slug}")]
    public async Task<ActionResult<VlogDto>> GetVlog(string slug, CancellationToken cancellationToken)
    {
        var vlog = await db.Vlogs.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
                    ?? throw ApiException.NotFound("Vlog");
        return vlog.ToDto();
    }

    [HttpPost("vlogs/{slug}/view")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> TrackVlogView(string slug, CancellationToken cancellationToken)
    {
        var vlog = await db.Vlogs.FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
                    ?? throw ApiException.NotFound("Vlog");
        vlog.ViewCount++;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("banners")]
    public async Task<ActionResult<IReadOnlyList<BannerDto>>> GetBanners([FromQuery] string? placement, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = db.Banners.AsNoTracking().Where(x => x.IsActive)
            .Where(x => (x.StartsAtUtc == null || x.StartsAtUtc <= now) && (x.EndsAtUtc == null || x.EndsAtUtc >= now));
        if (!string.IsNullOrWhiteSpace(placement)) query = query.Where(x => x.Placement == placement);
        var banners = await query.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return banners.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetFeaturedReviews([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.ProductReviews.AsNoTracking()
            .Where(x => x.IsActive && x.Status == ReviewStatus.Approved)
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ReviewDto>.Create(
            items.Select(x => x.ToDto(x.Product?.Name ?? "Makeup Art")).ToList(),
            request.Page, request.PageSize, total);
    }
}
