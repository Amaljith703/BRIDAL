using System.Linq.Expressions;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Domain.Content;
using MakeupArt.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<ContentSection> ContentSections => Set<ContentSection>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<MusicTrack> MusicTracks => Set<MusicTrack>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<Vlog> Vlogs => Set<Vlog>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductCombo> ProductCombos => Set<ProductCombo>();
    public DbSet<ProductComboItem> ProductComboItems => Set<ProductComboItem>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<ServicePackageItem> ServicePackageItems => Set<ServicePackageItem>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.FirstName).HasMaxLength(80);
            b.Property(x => x.LastName).HasMaxLength(80);
            b.Property(x => x.DisplayName).HasMaxLength(160);
            b.HasIndex(x => x.IsActive);
        });

        builder.Entity<SiteSetting>(b =>
        {
            b.Property(x => x.BusinessName).HasMaxLength(200);
            b.Property(x => x.Currency).HasMaxLength(8);
            b.Property(x => x.GstNumber).HasMaxLength(30);
        });
        builder.Entity<Page>(b =>
        {
            b.HasIndex(x => x.Key).IsUnique();
            b.Property(x => x.Key).HasMaxLength(120);
            b.Property(x => x.Title).HasMaxLength(200);
        });
        builder.Entity<ContentSection>(b =>
        {
            b.HasIndex(x => new { x.PageId, x.Key });
            b.HasOne(x => x.Page).WithMany(x => x.Sections).HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Key).HasMaxLength(120);
            b.Property(x => x.SectionType).HasMaxLength(80);
        });
        builder.Entity<MusicTrack>(b => b.HasIndex(x => new { x.IsDefault, x.SortOrder }));
        builder.Entity<Testimonial>(b => b.HasIndex(x => new { x.Status, x.SortOrder }));
        builder.Entity<Faq>(b => b.HasIndex(x => new { x.Category, x.SortOrder }));
        builder.Entity<PortfolioItem>(b => b.HasIndex(x => new { x.Category, x.SortOrder }));
        builder.Entity<Vlog>(b =>
        {
            b.HasIndex(x => x.Slug).IsUnique();
            b.Property(x => x.Slug).HasMaxLength(150);
            b.HasIndex(x => new { x.Type, x.PublishedAtUtc });
        });
        builder.Entity<MediaAsset>(b =>
        {
            b.HasIndex(x => x.Folder);
            b.Property(x => x.Url).HasMaxLength(1000);
        });
        builder.Entity<AnalyticsEvent>(b =>
        {
            b.HasIndex(x => x.OccurredAtUtc);
            b.HasIndex(x => new { x.EventType, x.Path });
        });
        builder.Entity<BackupRecord>(b => b.HasIndex(x => x.StartedAtUtc));
        builder.Entity<SiteSetting>()
            .HasOne(x => x.DefaultMusicTrack).WithMany().HasForeignKey(x => x.DefaultMusicTrackId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Category>(b =>
        {
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Product>(b =>
        {
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => x.Sku).IsUnique();
            b.HasIndex(x => new { x.IsActive, x.IsFeatured, x.SortOrder });
            b.HasIndex(x => new { x.CategoryId, x.IsActive });
            b.HasIndex(x => x.Price);
            b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.TaxRate).WithMany().HasForeignKey(x => x.TaxRateId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ProductImage>(b =>
        {
            b.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.ProductId, x.SortOrder });
        });
        builder.Entity<ProductVariant>(b =>
        {
            b.HasOne(x => x.Product).WithMany(x => x.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.Sku).IsUnique();
        });
        builder.Entity<ProductCombo>(b => b.HasIndex(x => x.Slug).IsUnique());
        builder.Entity<ProductComboItem>(b =>
        {
            b.HasOne(x => x.Combo).WithMany(x => x.Items).HasForeignKey(x => x.ComboId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Package).WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ServiceCategory>(b => b.HasIndex(x => x.Slug).IsUnique());
        builder.Entity<Service>(b =>
        {
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => new { x.IsActive, x.IsFeatured, x.SortOrder });
            b.HasOne(x => x.Category).WithMany(x => x.Services).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.TaxRate).WithMany().HasForeignKey(x => x.TaxRateId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ServicePackage>(b =>
        {
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasOne(x => x.TaxRate).WithMany().HasForeignKey(x => x.TaxRateId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ServicePackageItem>(b =>
        {
            b.HasOne(x => x.Package).WithMany(x => x.Items).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Coupon>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Code).HasMaxLength(40);
        });
        builder.Entity<CouponRedemption>(b =>
        {
            b.HasIndex(x => new { x.CouponId, x.UserId });
            b.HasOne(x => x.Coupon).WithMany(x => x.Redemptions).HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ProductReview>(b =>
        {
            b.HasIndex(x => new { x.ProductId, x.Status });
            b.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
            b.HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CustomerAddress>(b =>
        {
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.UserId, x.IsActive });
        });
        builder.Entity<Order>(b =>
        {
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Invoice).WithOne(x => x.Order).HasForeignKey<Invoice>(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<OrderItem>(b =>
        {
            b.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.OrderId, x.ItemType });
        });
        builder.Entity<OrderStatusHistory>(b =>
        {
            b.HasOne(x => x.Order).WithMany(x => x.StatusHistory).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Payment>(b =>
        {
            b.HasOne(x => x.Order).WithMany(x => x.Payments).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.ProviderOrderId);
            b.HasIndex(x => x.ProviderPaymentId);
        });
        builder.Entity<Refund>(b =>
        {
            b.HasOne(x => x.Order).WithMany(x => x.Refunds).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Payment).WithMany(x => x.Refunds).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Invoice>(b => b.HasIndex(x => x.InvoiceNumber).IsUnique());
        builder.Entity<BookingRequest>(b =>
        {
            b.HasIndex(x => x.BookingNumber).IsUnique();
            b.HasIndex(x => new { x.Status, x.PreferredDate });
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Package).WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ContactMessage>(b => b.HasIndex(x => new { x.Status, x.CreatedAtUtc }));
        builder.Entity<SupportTicket>(b =>
        {
            b.HasIndex(x => x.TicketNumber).IsUnique();
            b.HasIndex(x => new { x.Status, x.LastMessageAtUtc });
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SupportMessage>(b =>
        {
            b.HasOne(x => x.Ticket).WithMany(x => x.Messages).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.TicketId, x.CreatedAtUtc });
        });
        builder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AuditLog>(b => b.HasIndex(x => new { x.EntityName, x.CreatedAtUtc }));

        // Global rules for every auditable business entity.
        foreach (var entityType in builder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            var entity = builder.Entity(entityType.ClrType);
            entity.Property(nameof(BaseEntity.CreatedAtUtc)).HasColumnType("datetime2");
            entity.Property(nameof(BaseEntity.UpdatedAtUtc)).HasColumnType("datetime2");
            entity.Property(nameof(BaseEntity.DeletedAtUtc)).HasColumnType("datetime2");
            entity.Property(nameof(BaseEntity.RowVersion)).IsRowVersion();
            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                entity.Property(property.Name).HasPrecision(18, 2);

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                Expression.Constant(false));
            entity.HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditRules();
        return base.SaveChanges();
    }

    private void ApplyAuditRules()
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.CreatedBy ??= userId;
                entry.Entity.UpdatedBy ??= userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = userId ?? entry.Entity.UpdatedBy;
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.IsActive = false;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = userId ?? entry.Entity.UpdatedBy;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }

    private Guid? GetUserId()
    {
        var value = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
