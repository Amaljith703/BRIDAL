using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Application.Dtos.Catalog;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers.Admin;

[Authorize(Policy = "Staff")]
[Route("api/admin/catalog")]
public class AdminCatalogController(AppDbContext db) : BaseApiController
{
    // ── Categories ─────────────────────────────────────────────────────────
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var items = await db.Categories.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return items.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CategoryRequest request, CancellationToken cancellationToken)
    {
        var slugs = await db.Categories.IgnoreQueryFilters().Select(x => x.Slug).ToListAsync(cancellationToken);
        var category = new Category
        {
            Name = request.Name,
            Slug = Slugger.Unique(request.Slug ?? request.Name, slugs),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Category");
        var slugs = await db.Categories.IgnoreQueryFilters().Where(x => x.Id != id).Select(x => x.Slug).ToListAsync(cancellationToken);
        category.Name = request.Name;
        category.Slug = Slugger.Unique(request.Slug ?? request.Name, slugs);
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.ParentId = request.ParentId == id ? null : request.ParentId;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Category");
        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Service categories ─────────────────────────────────────────────────
    [HttpGet("service-categories")]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryDto>>> GetServiceCategories(CancellationToken cancellationToken)
    {
        var items = await db.ServiceCategories.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return items.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("service-categories")]
    public async Task<ActionResult<ServiceCategoryDto>> CreateServiceCategory(CategoryRequest request, CancellationToken cancellationToken)
    {
        var slugs = await db.ServiceCategories.IgnoreQueryFilters().Select(x => x.Slug).ToListAsync(cancellationToken);
        var category = new ServiceCategory
        {
            Name = request.Name,
            Slug = Slugger.Unique(request.Slug ?? request.Name, slugs),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    [HttpPut("service-categories/{id:guid}")]
    public async Task<ActionResult<ServiceCategoryDto>> UpdateServiceCategory(Guid id, CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.ServiceCategories.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Service category");
        var slugs = await db.ServiceCategories.IgnoreQueryFilters().Where(x => x.Id != id).Select(x => x.Slug).ToListAsync(cancellationToken);
        category.Name = request.Name;
        category.Slug = Slugger.Unique(request.Slug ?? request.Name, slugs);
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    [HttpDelete("service-categories/{id:guid}")]
    public async Task<IActionResult> DeleteServiceCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.ServiceCategories.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Service category");
        db.ServiceCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Products ───────────────────────────────────────────────────────────
    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] PagedRequest request, [FromQuery] Guid? categoryId, [FromQuery] bool? lowStock,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().Include(x => x.Category).AsQueryable();
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId);
        if (lowStock is true) query = query.Where(x => x.StockQuantity <= x.LowStockThreshold);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Sku.Contains(term) || (x.Brand != null && x.Brand.Contains(term)));
        }
        if (request.IsActive is not null) query = query.Where(x => x.IsActive == request.IsActive);
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.IsAscending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            "price" => request.IsAscending ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price),
            "stock" => request.IsAscending ? query.OrderBy(x => x.StockQuantity) : query.OrderByDescending(x => x.StockQuantity),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ProductDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).Include(x => x.Reviews).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Product");
        return product.ToDetailDto();
    }

    [HttpPost("products")]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product();
        await Apply(product, request, cancellationToken);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadProduct(product.Id, cancellationToken);
    }

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.Include(x => x.Images).Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Product");
        await Apply(product, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadProduct(product.Id, cancellationToken);
    }

    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Product");
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Services ───────────────────────────────────────────────────────────
    [HttpGet("services")]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetServices([FromQuery] PagedRequest request, [FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = db.Services.AsNoTracking().Include(x => x.Category).AsQueryable();
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }
        if (request.IsActive is not null) query = query.Where(x => x.IsActive == request.IsActive);
        query = query.OrderBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ServiceDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("services")]
    public async Task<ActionResult<ServiceDto>> CreateService(ServiceRequest request, CancellationToken cancellationToken)
    {
        var service = new Service();
        await Apply(service, request, cancellationToken);
        db.Services.Add(service);
        await db.SaveChangesAsync(cancellationToken);
        service.Category = await db.ServiceCategories.FindAsync([service.CategoryId], cancellationToken);
        return service.ToDto();
    }

    [HttpPut("services/{id:guid}")]
    public async Task<ActionResult<ServiceDto>> UpdateService(Guid id, ServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await db.Services.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Service");
        await Apply(service, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(service).Reference(x => x.Category).LoadAsync(cancellationToken);
        return service.ToDto();
    }

    [HttpDelete("services/{id:guid}")]
    public async Task<IActionResult> DeleteService(Guid id, CancellationToken cancellationToken)
    {
        var service = await db.Services.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Service");
        db.Services.Remove(service);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Packages ───────────────────────────────────────────────────────────
    [HttpGet("packages")]
    public async Task<ActionResult<PagedResult<ServicePackageDto>>> GetPackages([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.ServicePackages.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Service).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }
        if (request.IsActive is not null) query = query.Where(x => x.IsActive == request.IsActive);
        query = query.OrderBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ServicePackageDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("packages")]
    public async Task<ActionResult<ServicePackageDto>> CreatePackage(ServicePackageRequest request, CancellationToken cancellationToken)
    {
        var package = new ServicePackage();
        await Apply(package, request, cancellationToken);
        db.ServicePackages.Add(package);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadPackage(package.Id, cancellationToken);
    }

    [HttpPut("packages/{id:guid}")]
    public async Task<ActionResult<ServicePackageDto>> UpdatePackage(Guid id, ServicePackageRequest request, CancellationToken cancellationToken)
    {
        var package = await db.ServicePackages.Include(x => x.Items).ThenInclude(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw ApiException.NotFound("Package");
        await Apply(package, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadPackage(package.Id, cancellationToken);
    }

    [HttpDelete("packages/{id:guid}")]
    public async Task<IActionResult> DeletePackage(Guid id, CancellationToken cancellationToken)
    {
        var package = await db.ServicePackages.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Package");
        db.ServicePackages.Remove(package);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Combos ─────────────────────────────────────────────────────────────
    [HttpGet("combos")]
    public async Task<ActionResult<PagedResult<ComboDto>>> GetCombos([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.ProductCombos.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Items).ThenInclude(x => x.Service)
            .Include(x => x.Items).ThenInclude(x => x.Package).AsQueryable();
        if (request.IsActive is not null) query = query.Where(x => x.IsActive == request.IsActive);
        query = query.OrderBy(x => x.SortOrder);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ComboDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("combos")]
    public async Task<ActionResult<ComboDto>> CreateCombo(ComboRequest request, CancellationToken cancellationToken)
    {
        var combo = new ProductCombo();
        await Apply(combo, request, cancellationToken);
        db.ProductCombos.Add(combo);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadCombo(combo.Id, cancellationToken);
    }

    [HttpPut("combos/{id:guid}")]
    public async Task<ActionResult<ComboDto>> UpdateCombo(Guid id, ComboRequest request, CancellationToken cancellationToken)
    {
        var combo = await db.ProductCombos.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw ApiException.NotFound("Combo");
        await Apply(combo, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadCombo(combo.Id, cancellationToken);
    }

    [HttpDelete("combos/{id:guid}")]
    public async Task<IActionResult> DeleteCombo(Guid id, CancellationToken cancellationToken)
    {
        var combo = await db.ProductCombos.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Combo");
        db.ProductCombos.Remove(combo);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Offers ─────────────────────────────────────────────────────────────
    [HttpGet("offers")]
    public async Task<ActionResult<IReadOnlyList<OfferDto>>> GetOffers(CancellationToken cancellationToken)
    {
        var offers = await db.Offers.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return offers.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("offers")]
    public async Task<ActionResult<OfferDto>> CreateOffer(OfferRequest request, CancellationToken cancellationToken)
    {
        var offer = new Offer();
        Apply(offer, request);
        db.Offers.Add(offer);
        await db.SaveChangesAsync(cancellationToken);
        return offer.ToDto();
    }

    [HttpPut("offers/{id:guid}")]
    public async Task<ActionResult<OfferDto>> UpdateOffer(Guid id, OfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await db.Offers.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Offer");
        Apply(offer, request);
        await db.SaveChangesAsync(cancellationToken);
        return offer.ToDto();
    }

    [HttpDelete("offers/{id:guid}")]
    public async Task<IActionResult> DeleteOffer(Guid id, CancellationToken cancellationToken)
    {
        var offer = await db.Offers.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Offer");
        db.Offers.Remove(offer);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Coupons ────────────────────────────────────────────────────────────
    [HttpGet("coupons")]
    public async Task<ActionResult<PagedResult<CouponDto>>> GetCoupons([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.Coupons.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Code.Contains(request.Search.Trim()));
        query = query.OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<CouponDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("coupons")]
    public async Task<ActionResult<CouponDto>> CreateCoupon(CouponRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Coupons.IgnoreQueryFilters().AnyAsync(x => x.Code == code, cancellationToken))
            throw ApiException.Conflict("That coupon code already exists.");
        var coupon = new Coupon();
        Apply(coupon, request, code);
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync(cancellationToken);
        return coupon.ToDto();
    }

    [HttpPut("coupons/{id:guid}")]
    public async Task<ActionResult<CouponDto>> UpdateCoupon(Guid id, CouponRequest request, CancellationToken cancellationToken)
    {
        var coupon = await db.Coupons.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Coupon");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Coupons.IgnoreQueryFilters().AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw ApiException.Conflict("That coupon code already exists.");
        Apply(coupon, request, code);
        await db.SaveChangesAsync(cancellationToken);
        return coupon.ToDto();
    }

    [HttpDelete("coupons/{id:guid}")]
    public async Task<IActionResult> DeleteCoupon(Guid id, CancellationToken cancellationToken)
    {
        var coupon = await db.Coupons.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Coupon");
        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Tax rates ──────────────────────────────────────────────────────────
    [HttpGet("tax-rates")]
    public async Task<ActionResult<IReadOnlyList<TaxRateDto>>> GetTaxRates(CancellationToken cancellationToken)
    {
        var rates = await db.TaxRates.AsNoTracking().OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return rates.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("tax-rates")]
    public async Task<ActionResult<TaxRateDto>> CreateTaxRate(TaxRateRequest request, CancellationToken cancellationToken)
    {
        var rate = new TaxRate();
        Apply(rate, request);
        db.TaxRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        return rate.ToDto();
    }

    [HttpPut("tax-rates/{id:guid}")]
    public async Task<ActionResult<TaxRateDto>> UpdateTaxRate(Guid id, TaxRateRequest request, CancellationToken cancellationToken)
    {
        var rate = await db.TaxRates.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Tax rate");
        Apply(rate, request);
        await db.SaveChangesAsync(cancellationToken);
        return rate.ToDto();
    }

    [HttpDelete("tax-rates/{id:guid}")]
    public async Task<IActionResult> DeleteTaxRate(Guid id, CancellationToken cancellationToken)
    {
        var rate = await db.TaxRates.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Tax rate");
        db.TaxRates.Remove(rate);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Shipping methods ───────────────────────────────────────────────────
    [HttpGet("shipping-methods")]
    public async Task<ActionResult<IReadOnlyList<ShippingMethodDto>>> GetShippingMethods(CancellationToken cancellationToken)
    {
        var items = await db.ShippingMethods.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return items.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("shipping-methods")]
    public async Task<ActionResult<ShippingMethodDto>> CreateShippingMethod(ShippingMethodRequest request, CancellationToken cancellationToken)
    {
        if (request.IsDefault)
        {
            var existing = await db.ShippingMethods.Where(x => x.IsDefault).ToListAsync(cancellationToken);
            foreach (var method in existing) method.IsDefault = false;
        }
        var item = new ShippingMethod();
        Apply(item, request);
        db.ShippingMethods.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.ToDto();
    }

    [HttpPut("shipping-methods/{id:guid}")]
    public async Task<ActionResult<ShippingMethodDto>> UpdateShippingMethod(Guid id, ShippingMethodRequest request, CancellationToken cancellationToken)
    {
        var item = await db.ShippingMethods.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Shipping method");
        if (request.IsDefault)
        {
            var existing = await db.ShippingMethods.Where(x => x.IsDefault && x.Id != id).ToListAsync(cancellationToken);
            foreach (var method in existing) method.IsDefault = false;
        }
        Apply(item, request);
        await db.SaveChangesAsync(cancellationToken);
        return item.ToDto();
    }

    [HttpDelete("shipping-methods/{id:guid}")]
    public async Task<IActionResult> DeleteShippingMethod(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.ShippingMethods.FindAsync([id], cancellationToken) ?? throw ApiException.NotFound("Shipping method");
        db.ShippingMethods.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private async Task Apply(Product product, ProductRequest r, CancellationToken cancellationToken)
    {
        if (r.Price < 0) throw ApiException.Validation("Price cannot be negative.");
        var slugs = await db.Products.IgnoreQueryFilters().Where(x => x.Id != product.Id).Select(x => x.Slug).ToListAsync(cancellationToken);
        var skus = await db.Products.IgnoreQueryFilters().Where(x => x.Id != product.Id).Select(x => x.Sku).ToListAsync(cancellationToken);
        if (skus.Contains(r.Sku, StringComparer.OrdinalIgnoreCase) ||
            await db.ProductVariants.IgnoreQueryFilters().AnyAsync(x => x.Sku == r.Sku && x.ProductId != product.Id, cancellationToken))
            throw ApiException.Conflict($"The SKU '{r.Sku}' is already in use.");
        if (r.CategoryId is not null && !await db.Categories.AnyAsync(x => x.Id == r.CategoryId, cancellationToken))
            throw ApiException.Validation("Choose a valid category.");
        if (r.TaxRateId is not null && !await db.TaxRates.AnyAsync(x => x.Id == r.TaxRateId, cancellationToken))
            throw ApiException.Validation("Choose a valid tax rate.");

        product.Name = r.Name;
        product.Slug = Slugger.Unique(r.Slug ?? r.Name, slugs);
        product.Sku = r.Sku;
        product.ShortDescription = r.ShortDescription;
        product.Description = r.Description;
        product.CategoryId = r.CategoryId;
        product.Brand = r.Brand;
        product.Price = r.Price;
        product.CompareAtPrice = r.CompareAtPrice;
        product.CostPrice = r.CostPrice;
        product.TaxRateId = r.TaxRateId;
        product.StockQuantity = Math.Max(0, r.StockQuantity);
        product.LowStockThreshold = Math.Max(0, r.LowStockThreshold);
        product.IsFeatured = r.IsFeatured;
        product.IsPromoted = r.IsPromoted;
        product.IsNewArrival = r.IsNewArrival;
        product.ImageUrl = r.ImageUrl;
        product.TagsCsv = r.TagsCsv;
        product.WeightGrams = Math.Max(0, r.WeightGrams);
        product.HsnCode = r.HsnCode;
        product.SeoTitle = r.SeoTitle;
        product.SeoDescription = r.SeoDescription;
        product.SortOrder = r.SortOrder;
        product.IsActive = r.IsActive;

        var existingImages = product.Images.ToList();
        foreach (var requestImage in r.Images)
        {
            var existing = existingImages.FirstOrDefault(x => x.Url == requestImage.Url);
            if (existing is null)
            {
                var image = new ProductImage { ProductId = product.Id, Url = requestImage.Url, AltText = requestImage.AltText, SortOrder = requestImage.SortOrder, IsActive = true };
                db.ProductImages.Add(image);
                product.Images.Add(image);
            }
            else
            {
                existing.AltText = requestImage.AltText;
                existing.SortOrder = requestImage.SortOrder;
                existing.IsActive = true;
            }
            existingImages.RemoveAll(x => x.Url == requestImage.Url);
        }
        foreach (var removed in existingImages) db.ProductImages.Remove(removed);

        var existingVariants = product.Variants.ToList();
        var requestSkus = r.Variants.Select(v => v.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var requestVariant in r.Variants)
        {
            var variant = existingVariants.FirstOrDefault(x => string.Equals(x.Sku, requestVariant.Sku, StringComparison.OrdinalIgnoreCase));
            if (variant is null)
            {
                var newVariant = new ProductVariant
                {
                    ProductId = product.Id,
                    Name = requestVariant.Name,
                    Sku = requestVariant.Sku,
                    PriceOverride = requestVariant.PriceOverride,
                    StockQuantity = Math.Max(0, requestVariant.StockQuantity),
                    ImageUrl = requestVariant.ImageUrl,
                    SortOrder = requestVariant.SortOrder,
                    IsActive = true
                };
                db.ProductVariants.Add(newVariant);
                product.Variants.Add(newVariant);
            }
            else
            {
                variant.Name = requestVariant.Name;
                variant.PriceOverride = requestVariant.PriceOverride;
                variant.StockQuantity = Math.Max(0, requestVariant.StockQuantity);
                variant.ImageUrl = requestVariant.ImageUrl;
                variant.SortOrder = requestVariant.SortOrder;
                variant.IsActive = true;
            }
        }
        foreach (var removed in existingVariants.Where(x => !requestSkus.Contains(x.Sku))) db.ProductVariants.Remove(removed);
    }

    private async Task Apply(Service service, ServiceRequest r, CancellationToken cancellationToken)
    {
        var slugs = await db.Services.IgnoreQueryFilters().Where(x => x.Id != service.Id).Select(x => x.Slug).ToListAsync(cancellationToken);
        service.Name = r.Name;
        service.Slug = Slugger.Unique(r.Slug ?? r.Name, slugs);
        service.ShortDescription = r.ShortDescription;
        service.Description = r.Description;
        service.CategoryId = r.CategoryId;
        service.Price = r.Price;
        service.CompareAtPrice = r.CompareAtPrice;
        service.DurationMinutes = r.DurationMinutes;
        service.ImageUrl = r.ImageUrl;
        service.IsFeatured = r.IsFeatured;
        service.TaxRateId = r.TaxRateId;
        service.HsnCode = r.HsnCode;
        service.SeoTitle = r.SeoTitle;
        service.SeoDescription = r.SeoDescription;
        service.SortOrder = r.SortOrder;
        service.IsActive = r.IsActive;
    }

    private async Task Apply(ServicePackage package, ServicePackageRequest r, CancellationToken cancellationToken)
    {
        var slugs = await db.ServicePackages.IgnoreQueryFilters().Where(x => x.Id != package.Id).Select(x => x.Slug).ToListAsync(cancellationToken);
        package.Name = r.Name;
        package.Slug = Slugger.Unique(r.Slug ?? r.Name, slugs);
        package.ShortDescription = r.ShortDescription;
        package.Description = r.Description;
        package.Price = r.Price;
        package.CompareAtPrice = r.CompareAtPrice;
        package.DurationMinutes = r.DurationMinutes;
        package.ImageUrl = r.ImageUrl;
        package.Occasion = r.Occasion;
        package.IsFeatured = r.IsFeatured;
        package.TaxRateId = r.TaxRateId;
        package.HsnCode = r.HsnCode;
        package.SortOrder = r.SortOrder;
        package.IsActive = r.IsActive;

        var serviceIds = r.Items.Select(x => x.ServiceId).ToList();
        var validServices = await db.Services.Where(x => serviceIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        if (validServices.Count != serviceIds.Distinct().Count())
            throw ApiException.Validation("One or more package services do not exist.");

        var existing = package.Items.ToList();
        var requestedIds = r.Items.Select(x => x.ServiceId).ToHashSet();
        foreach (var item in r.Items)
        {
            var current = existing.FirstOrDefault(x => x.ServiceId == item.ServiceId);
            if (current is null)
            {
                var newItem = new ServicePackageItem { PackageId = package.Id, ServiceId = item.ServiceId, Quantity = item.Quantity, SortOrder = item.SortOrder };
                db.ServicePackageItems.Add(newItem);
                package.Items.Add(newItem);
            }
            else
            {
                current.Quantity = item.Quantity;
                current.SortOrder = item.SortOrder;
                current.IsActive = true;
            }
        }
        foreach (var removed in existing.Where(x => !requestedIds.Contains(x.ServiceId))) db.ServicePackageItems.Remove(removed);
    }

    private async Task Apply(ProductCombo combo, ComboRequest r, CancellationToken cancellationToken)
    {
        var slugs = await db.ProductCombos.IgnoreQueryFilters().Where(x => x.Id != combo.Id).Select(x => x.Slug).ToListAsync(cancellationToken);
        combo.Name = r.Name;
        combo.Slug = Slugger.Unique(r.Slug ?? r.Name, slugs);
        combo.Description = r.Description;
        combo.Price = r.Price;
        combo.CompareAtPrice = r.CompareAtPrice;
        combo.ImageUrl = r.ImageUrl;
        combo.IsFeatured = r.IsFeatured;
        combo.SortOrder = r.SortOrder;
        combo.IsActive = r.IsActive;

        foreach (var item in r.Items)
        {
            if (item.ProductId is null && item.ServiceId is null && item.PackageId is null)
                throw ApiException.Validation("Each combo item needs a product, service or package.");
        }
        db.ProductComboItems.RemoveRange(combo.Items);
        combo.Items = r.Items.Select(x => new ProductComboItem
        {
            ComboId = combo.Id,
            ProductId = x.ProductId,
            ServiceId = x.ServiceId,
            PackageId = x.PackageId,
            Quantity = x.Quantity,
            SortOrder = x.SortOrder
        }).ToList();
        await Task.CompletedTask;
    }

    private static void Apply(Offer offer, OfferRequest r)
    {
        offer.Title = r.Title;
        offer.Subtitle = r.Subtitle;
        offer.Description = r.Description;
        offer.Code = string.IsNullOrWhiteSpace(r.Code) ? null : r.Code.Trim().ToUpperInvariant();
        offer.DiscountType = r.DiscountType;
        offer.DiscountValue = r.DiscountValue;
        offer.ImageUrl = r.ImageUrl;
        offer.AppliesTo = r.AppliesTo;
        offer.StartsAtUtc = r.StartsAtUtc;
        offer.EndsAtUtc = r.EndsAtUtc;
        offer.IsFeatured = r.IsFeatured;
        offer.SortOrder = r.SortOrder;
        offer.IsActive = r.IsActive;
    }

    private static void Apply(Coupon coupon, CouponRequest r, string code)
    {
        coupon.Code = code;
        coupon.Description = r.Description;
        coupon.DiscountType = r.DiscountType;
        coupon.DiscountValue = r.DiscountValue;
        coupon.MinimumOrderAmount = r.MinimumOrderAmount;
        coupon.MaximumDiscountAmount = r.MaximumDiscountAmount;
        coupon.Scope = r.Scope;
        coupon.StartsAtUtc = r.StartsAtUtc;
        coupon.EndsAtUtc = r.EndsAtUtc;
        coupon.UsageLimit = r.UsageLimit;
        coupon.PerUserLimit = r.PerUserLimit;
        coupon.IsActive = r.IsActive;
    }

    private static void Apply(TaxRate rate, TaxRateRequest r)
    {
        rate.Name = r.Name;
        rate.RatePercent = r.RatePercent;
        rate.Country = r.Country;
        rate.State = r.State;
        rate.HsnCode = r.HsnCode;
        rate.Description = r.Description;
        rate.IsDefault = r.IsDefault;
        rate.IsActive = r.IsActive;
    }

    private static void Apply(ShippingMethod method, ShippingMethodRequest r)
    {
        method.Name = r.Name;
        method.Description = r.Description;
        method.Price = r.Price;
        method.FreeAboveAmount = r.FreeAboveAmount;
        method.EstimatedDays = r.EstimatedDays;
        method.IsDefault = r.IsDefault;
        method.SortOrder = r.SortOrder;
        method.IsActive = r.IsActive;
    }

    private async Task<ProductDetailDto> LoadProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).Include(x => x.Reviews).ThenInclude(x => x.User)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return product.ToDetailDto();
    }

    private async Task<ServicePackageDto> LoadPackage(Guid id, CancellationToken cancellationToken)
    {
        var package = await db.ServicePackages.AsNoTracking().Include(x => x.Items).ThenInclude(x => x.Service)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return package.ToDto();
    }

    private async Task<ComboDto> LoadCombo(Guid id, CancellationToken cancellationToken)
    {
        var combo = await db.ProductCombos.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Items).ThenInclude(x => x.Service)
            .Include(x => x.Items).ThenInclude(x => x.Package)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return combo.ToDto();
    }
}
