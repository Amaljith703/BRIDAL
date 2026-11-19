using System.Linq.Expressions;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Catalog;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Common;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers;

[Route("api/catalog")]
public class CatalogController(AppDbContext db) : BaseApiController
{
    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] PagedRequest request,
        [FromQuery] string? category,
        [FromQuery] bool? featured,
        [FromQuery] bool? newArrival,
        [FromQuery] bool? promoted,
        [FromQuery] string? tag,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minRating,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().Where(x => x.IsActive).Include(x => x.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Category != null && x.Category.Slug == category);
        if (featured is true) query = query.Where(x => x.IsFeatured);
        if (newArrival is true) query = query.Where(x => x.IsNewArrival);
        if (promoted is true) query = query.Where(x => x.IsPromoted);
        if (minPrice is not null) query = query.Where(x => x.Price >= minPrice);
        if (maxPrice is not null) query = query.Where(x => x.Price <= maxPrice);
        if (minRating is not null) query = query.Where(x => x.RatingAverage >= minRating);
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var term = tag.Trim();
            query = query.Where(x => x.TagsCsv != null && x.TagsCsv.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.ShortDescription != null && x.ShortDescription.Contains(term))
                || (x.TagsCsv != null && x.TagsCsv.Contains(term)) || (x.Brand != null && x.Brand.Contains(term)));
        }
        query = ApplyProductSort(query, request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ProductDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("products/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(string slug, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.Variants)
            .Include(x => x.Reviews).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
            ?? throw ApiException.NotFound("Product");
        product.Reviews = product.Reviews.Where(x => x.IsActive && x.Status == ReviewStatus.Approved).ToList();
        return product.ToDetailDto();
    }

    [HttpGet("products/{slug}/related")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetRelatedProducts(string slug, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
                      ?? throw ApiException.NotFound("Product");
        var related = await db.Products.AsNoTracking()
            .Where(x => x.IsActive && x.Id != product.Id && (product.CategoryId == null || x.CategoryId == product.CategoryId))
            .Include(x => x.Category)
            .OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder).Take(4)
            .ToListAsync(cancellationToken);
        if (related.Count < 4)
        {
            var extra = await db.Products.AsNoTracking()
                .Where(x => x.IsActive && x.Id != product.Id && related.Select(r => r.Id).Contains(x.Id) == false)
                .Include(x => x.Category)
                .OrderByDescending(x => x.RatingAverage).Take(4 - related.Count)
                .ToListAsync(cancellationToken);
            related.AddRange(extra);
        }
        return related.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return categories.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("services")]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetServices(
        [FromQuery] PagedRequest request,
        [FromQuery] string? category,
        [FromQuery] bool? featured,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        CancellationToken cancellationToken)
    {
        var query = db.Services.AsNoTracking().Where(x => x.IsActive).Include(x => x.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Category != null && x.Category.Slug == category);
        if (featured is true) query = query.Where(x => x.IsFeatured);
        if (minPrice is not null) query = query.Where(x => x.Price >= minPrice);
        if (maxPrice is not null) query = query.Where(x => x.Price <= maxPrice);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.ShortDescription != null && x.ShortDescription.Contains(term))
                || (x.Description != null && x.Description.Contains(term)));
        }
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price" => request.IsAscending ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price),
            "name" => request.IsAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            "duration" => request.IsAscending ? query.OrderBy(x => x.DurationMinutes) : query.OrderByDescending(x => x.DurationMinutes),
            _ => query.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ServiceDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("services/{slug}")]
    public async Task<ActionResult<ServiceDto>> GetService(string slug, CancellationToken cancellationToken)
    {
        var service = await db.Services.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
            ?? throw ApiException.NotFound("Service");
        return service.ToDto();
    }

    [HttpGet("service-categories")]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryDto>>> GetServiceCategories(CancellationToken cancellationToken)
    {
        var categories = await db.ServiceCategories.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return categories.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("packages")]
    public async Task<ActionResult<PagedResult<ServicePackageDto>>> GetPackages(
        [FromQuery] PagedRequest request, [FromQuery] string? occasion, [FromQuery] bool? featured,
        CancellationToken cancellationToken)
    {
        var query = db.ServicePackages.AsNoTracking().Where(x => x.IsActive)
            .Include(x => x.Items).ThenInclude(x => x.Service).AsQueryable();
        if (!string.IsNullOrWhiteSpace(occasion) && !occasion.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Occasion == occasion);
        if (featured is true) query = query.Where(x => x.IsFeatured);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price" => request.IsAscending ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price),
            "name" => request.IsAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            _ => query.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ServicePackageDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("packages/{slug}")]
    public async Task<ActionResult<ServicePackageDto>> GetPackage(string slug, CancellationToken cancellationToken)
    {
        var package = await db.ServicePackages.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Service)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
            ?? throw ApiException.NotFound("Package");
        return package.ToDto();
    }

    [HttpGet("combos")]
    public async Task<ActionResult<PagedResult<ComboDto>>> GetCombos([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.ProductCombos.AsNoTracking().Where(x => x.IsActive)
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Items).ThenInclude(x => x.Service)
            .Include(x => x.Items).ThenInclude(x => x.Package)
            .OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ComboDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("combos/{slug}")]
    public async Task<ActionResult<ComboDto>> GetCombo(string slug, CancellationToken cancellationToken)
    {
        var combo = await db.ProductCombos.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Items).ThenInclude(x => x.Service)
            .Include(x => x.Items).ThenInclude(x => x.Package)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, cancellationToken)
            ?? throw ApiException.NotFound("Combo");
        return combo.ToDto();
    }

    [HttpGet("offers")]
    public async Task<ActionResult<IReadOnlyList<OfferDto>>> GetOffers(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var offers = await db.Offers.AsNoTracking()
            .Where(x => x.IsActive && (x.StartsAtUtc == null || x.StartsAtUtc <= now) && (x.EndsAtUtc == null || x.EndsAtUtc >= now))
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return offers.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("shipping-methods")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetShippingMethods(CancellationToken cancellationToken)
    {
        var methods = await db.ShippingMethods.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).Select(x => new { x.Id, x.Name, x.Description, x.Price, x.FreeAboveAmount, x.EstimatedDays, x.IsDefault })
            .ToListAsync(cancellationToken);
        return Ok(methods);
    }

    private static IQueryable<Product> ApplyProductSort(IQueryable<Product> query, PagedRequest request)
        => request.SortBy?.ToLowerInvariant() switch
        {
            "price" => request.IsAscending ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price),
            "name" => request.IsAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            "rating" => query.OrderByDescending(x => x.RatingAverage).ThenByDescending(x => x.RatingCount),
            "newest" => query.OrderByDescending(x => x.CreatedAtUtc),
            "popular" => query.OrderByDescending(x => x.IsPromoted).ThenByDescending(x => x.RatingCount),
            _ => query.OrderByDescending(x => x.IsPromoted).ThenBy(x => x.SortOrder)
        };
}
