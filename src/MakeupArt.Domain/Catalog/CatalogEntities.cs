using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Domain.Identity;

namespace MakeupArt.Domain.Catalog;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
    public int SortOrder { get; set; }
    public List<Product> Products { get; set; } = [];
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? Brand { get; set; } = "Makeup Art";
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public Guid? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsFeatured { get; set; }
    public bool IsPromoted { get; set; }
    public bool IsNewArrival { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public string? ImageUrl { get; set; }
    public string? TagsCsv { get; set; }
    public int WeightGrams { get; set; } = 250;
    public string? HsnCode { get; set; } = "3304";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public int SortOrder { get; set; }
    public List<ProductImage> Images { get; set; } = [];
    public List<ProductVariant> Variants { get; set; } = [];
    public List<ProductReview> Reviews { get; set; } = [];
}

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
}

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

public class ProductCombo : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public List<ProductComboItem> Items { get; set; } = [];
}

public class ProductComboItem : BaseEntity
{
    public Guid ComboId { get; set; }
    public ProductCombo? Combo { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? ServiceId { get; set; }
    public Service? Service { get; set; }
    public Guid? PackageId { get; set; }
    public ServicePackage? Package { get; set; }
    public int Quantity { get; set; } = 1;
    public int SortOrder { get; set; }
}

public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public List<Service> Services { get; set; } = [];
}

public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public ServiceCategory? Category { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }
    public string? HsnCode { get; set; } = "9997";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public int SortOrder { get; set; }
}

public class ServicePackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public string? Occasion { get; set; } = "Bridal";
    public bool IsFeatured { get; set; }
    public Guid? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }
    public string? HsnCode { get; set; } = "9997";
    public int SortOrder { get; set; }
    public List<ServicePackageItem> Items { get; set; } = [];
}

public class ServicePackageItem : BaseEntity
{
    public Guid PackageId { get; set; }
    public ServicePackage? Package { get; set; }
    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }
    public int Quantity { get; set; } = 1;
    public int SortOrder { get; set; }
}

public class Offer : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public string? ImageUrl { get; set; }
    public DiscountAppliesTo AppliesTo { get; set; } = DiscountAppliesTo.All;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
}

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public CouponScope Scope { get; set; } = CouponScope.All;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddMonths(3);
    public int UsageLimit { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public int UsedCount { get; set; }
    public List<CouponRedemption> Redemptions { get; set; } = [];
}

public class CouponRedemption : BaseEntity
{
    public Guid CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime RedeemedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ProductReview : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid? OrderId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public bool IsVerifiedPurchase { get; set; }
    public string? AdminResponse { get; set; }
}
