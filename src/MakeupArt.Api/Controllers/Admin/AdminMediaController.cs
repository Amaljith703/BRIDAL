using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Content;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Common;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers.Admin;

[Authorize(Policy = "Staff")]
[Route("api/admin/media")]
public class AdminMediaController(
    AppDbContext db,
    IMediaStorageService storage,
    IWebHostEnvironment environment,
    ILogger<AdminMediaController> logger) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MediaAssetDto>>> GetMedia(
        [FromQuery] PagedRequest request,
        [FromQuery] MediaType? mediaType,
        [FromQuery] string? folder,
        CancellationToken cancellationToken)
    {
        var query = db.MediaAssets.AsNoTracking().Where(x => x.IsActive).AsQueryable();
        if (mediaType is not null) query = query.Where(x => x.MediaType == mediaType);
        if (!string.IsNullOrWhiteSpace(folder)) query = query.Where(x => x.Folder == folder);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.OriginalFileName.Contains(term) || (x.AltText != null && x.AltText.Contains(term))
                || (x.Credit != null && x.Credit.Contains(term)));
        }
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.IsAscending ? query.OrderBy(x => x.OriginalFileName) : query.OrderByDescending(x => x.OriginalFileName),
            "size" => request.IsAscending ? query.OrderBy(x => x.SizeBytes) : query.OrderByDescending(x => x.SizeBytes),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<MediaAssetDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost]
    [RequestSizeLimit(160 * 1024 * 1024)]
    public async Task<ActionResult<MediaAssetDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? folder,
        [FromForm] string? altText,
        [FromForm] string? credit,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0) throw ApiException.Validation("Choose a file to upload.");
        await using var stream = file.OpenReadStream();
        var asset = await storage.SaveAsync(stream, file.FileName, file.ContentType, folder ?? "general", altText, credit, cancellationToken);
        logger.LogInformation("Media {Url} uploaded by {User}", asset.Url, User.Identity?.Name);
        return asset.ToDto();
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MediaAssetDto>> UpdateMetadata(Guid id, MediaMetadataRequest request, CancellationToken cancellationToken)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw ApiException.NotFound("Media asset");
        asset.AltText = string.IsNullOrWhiteSpace(request.AltText) ? null : request.AltText.Trim();
        asset.Credit = string.IsNullOrWhiteSpace(request.Credit) ? null : request.Credit.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return asset.ToDto();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw ApiException.NotFound("Media asset");
        if (!asset.Url.StartsWith("/media/uploads/", StringComparison.OrdinalIgnoreCase))
            throw ApiException.Validation("Seed media is referenced by the demo content and cannot be deleted here. Replace the image instead.");
        var inUse = await IsReferencedAsync(asset.Url, cancellationToken);
        if (inUse)
            throw ApiException.Conflict("This media is still used by a page, product or service. Remove those references first.");
        storage.DeletePhysicalFile(asset);
        asset.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("folders")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetFolders(CancellationToken cancellationToken)
    {
        var folders = await db.MediaAssets.AsNoTracking().Where(x => x.IsActive)
            .Select(x => x.Folder).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        return folders;
    }

    /// <summary>Exposes the local upload directory used by the current API instance.</summary>
    [HttpGet("storage")]
    public IActionResult GetStorageInfo()
    {
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var uploads = Path.Combine(webRoot, "media", "uploads");
        return Ok(new
        {
            webRoot,
            uploads,
            exists = Directory.Exists(uploads),
            files = Directory.Exists(uploads)
                ? Directory.EnumerateFiles(uploads, "*", SearchOption.AllDirectories).Count()
                : 0
        });
    }

    private async Task<bool> IsReferencedAsync(string url, CancellationToken cancellationToken)
    {
        return await db.SiteSettings.AnyAsync(x => x.LogoUrl == url || x.FaviconUrl == url, cancellationToken)
               || await db.HeroSlides.AnyAsync(x => x.ModelImageUrl == url || x.BackgroundImageUrl == url, cancellationToken)
               || await db.Products.AnyAsync(x => x.ImageUrl == url, cancellationToken)
               || await db.ProductImages.AnyAsync(x => x.Url == url, cancellationToken)
               || await db.Services.AnyAsync(x => x.ImageUrl == url, cancellationToken)
               || await db.ServicePackages.AnyAsync(x => x.ImageUrl == url, cancellationToken)
               || await db.ProductCombos.AnyAsync(x => x.ImageUrl == url, cancellationToken)
               || await db.PortfolioItems.AnyAsync(x => x.ImageUrl == url || x.VideoUrl == url, cancellationToken)
               || await db.Testimonials.AnyAsync(x => x.ImageUrl == url || x.VideoUrl == url, cancellationToken)
               || await db.Vlogs.AnyAsync(x => x.ThumbnailUrl == url || x.VideoUrl == url, cancellationToken)
               || await db.Banners.AnyAsync(x => x.ImageUrl == url, cancellationToken)
               || await db.ContentSections.AnyAsync(x => x.MediaUrl == url || x.SecondaryMediaUrl == url, cancellationToken)
               || await db.MusicTracks.AnyAsync(x => x.Url == url, cancellationToken);
    }
}

public class MediaMetadataRequest
{
    [MaxLength(300)] public string? AltText { get; set; }
    [MaxLength(300)] public string? Credit { get; set; }
}
