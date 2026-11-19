using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers.Admin;

[Authorize(Policy = "Staff")]
[Route("api/admin")]
public class AdminDashboardController(AppDbContext db) : BaseApiController
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var start7 = today.AddDays(-6);
        var start30 = today.AddDays(-29);

        var paidStatuses = new[] { PaymentStatus.Captured };
        var revenueOrders = await db.Orders.AsNoTracking()
            .Where(x => x.PaymentStatus == PaymentStatus.Captured && x.Status != OrderStatus.Cancelled && x.Status != OrderStatus.Refunded)
            .Select(x => new { x.Id, x.TotalAmount, x.CreatedAtUtc, x.PaidAtUtc, x.Status, x.CustomerName, x.OrderNumber })
            .ToListAsync(cancellationToken);

        var revenueToday = revenueOrders.Where(x => (x.PaidAtUtc ?? x.CreatedAtUtc) >= today).Sum(x => x.TotalAmount);
        var revenue7 = revenueOrders.Where(x => (x.PaidAtUtc ?? x.CreatedAtUtc) >= start7).Sum(x => x.TotalAmount);
        var revenue30 = revenueOrders.Where(x => (x.PaidAtUtc ?? x.CreatedAtUtc) >= start30).Sum(x => x.TotalAmount);
        var orders30 = await db.Orders.AsNoTracking().CountAsync(x => x.CreatedAtUtc >= start30, cancellationToken);
        var ordersToday = await db.Orders.AsNoTracking().CountAsync(x => x.CreatedAtUtc >= today, cancellationToken);
        var avgOrder = revenueOrders.Where(x => (x.PaidAtUtc ?? x.CreatedAtUtc) >= start30).Select(x => x.TotalAmount).DefaultIfEmpty(0m).Average();

        var totalCustomers = await db.Users.AsNoTracking().CountAsync(x => x.IsActive && !x.IsDeleted, cancellationToken);
        var newCustomers30 = await db.Users.AsNoTracking().CountAsync(x => x.CreatedAtUtc >= start30 && !x.IsDeleted, cancellationToken);
        var pageViews30 = await db.AnalyticsEvents.AsNoTracking()
            .CountAsync(x => x.EventType == "page_view" && x.OccurredAtUtc >= start30, cancellationToken);

        var pendingOrders = await db.Orders.AsNoTracking().CountAsync(x => x.Status == OrderStatus.PaymentPending || x.Status == OrderStatus.Paid, cancellationToken);
        var lowStock = await db.Products.AsNoTracking().CountAsync(x => x.IsActive && x.StockQuantity <= x.LowStockThreshold, cancellationToken);
        var pendingReviews = await db.ProductReviews.AsNoTracking().CountAsync(x => x.Status == ReviewStatus.Pending, cancellationToken);
        var pendingBookings = await db.BookingRequests.AsNoTracking().CountAsync(x => x.Status == BookingStatus.Pending, cancellationToken);
        var openTickets = await db.SupportTickets.AsNoTracking().CountAsync(x =>
            x.Status == TicketStatus.Open || x.Status == TicketStatus.Pending, cancellationToken);

        var days = Enumerable.Range(0, 30).Select(i => start30.AddDays(i)).ToList();
        var revenueSeries = days.Select(day => new ChartPointDto(day.ToString("dd MMM"),
            revenueOrders.Where(x => (x.PaidAtUtc ?? x.CreatedAtUtc).Date == day).Sum(x => x.TotalAmount))).ToList();
        var orderSeries = days.Select(day => new ChartPointDto(day.ToString("dd MMM"),
            orders30 == 0 ? 0 : 0)).ToList();
        var ordersByDay = await db.Orders.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= start30)
            .Select(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        orderSeries = days.Select(day => new ChartPointDto(day.ToString("dd MMM"), ordersByDay.Count(x => x.Date == day))).ToList();

        var paidOrderIds = revenueOrders.Select(x => x.Id).ToHashSet();
        var items = await db.OrderItems.AsNoTracking()
            .Where(x => paidOrderIds.Contains(x.OrderId))
            .ToListAsync(cancellationToken);
        var topProducts = items.Where(x => x.ItemType == OrderItemType.Product)
            .GroupBy(x => new { x.ItemId, x.Name, x.ImageUrl })
            .Select(g => new TopItemDto(g.Key.ItemId, g.Key.Name, g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal), g.Key.ImageUrl))
            .OrderByDescending(x => x.Revenue).Take(5).ToList();
        var topServices = items.Where(x => x.ItemType is OrderItemType.Service or OrderItemType.Package)
            .GroupBy(x => new { x.ItemId, x.Name, x.ImageUrl })
            .Select(g => new TopItemDto(g.Key.ItemId, g.Key.Name, g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal), g.Key.ImageUrl))
            .OrderByDescending(x => x.Revenue).Take(5).ToList();

        var statuses = await db.Orders.AsNoTracking().GroupBy(x => x.Status)
            .Select(g => new StatusSliceDto(g.Key.ToString(), g.Count(), g.Sum(x => x.TotalAmount)))
            .ToListAsync(cancellationToken);
        var methods = await db.Payments.AsNoTracking().Where(x => x.Status == PaymentStatus.Captured)
            .GroupBy(x => x.Method ?? "unknown")
            .Select(g => new StatusSliceDto(g.Key, g.Count(), g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);
        var recent = await db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(8)
            .Select(x => new RecentOrderDto(x.Id, x.OrderNumber, x.CustomerName, x.TotalAmount, x.Status.ToString(), x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new DashboardDto(revenueToday, revenue7, revenue30, ordersToday, orders30, Math.Round(avgOrder, 2),
            totalCustomers, newCustomers30, pageViews30, pendingOrders, lowStock, pendingReviews, pendingBookings, openTickets,
            revenueSeries, orderSeries, topProducts, topServices, statuses, methods, recent,
            lowStock > 0 ? $"{lowStock} product{(lowStock == 1 ? "" : "s")} need restocking. Pending bookings and support tickets are waiting for a reply." : "Stock levels look healthy. Focus on the newest bookings and support replies this week.");
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetAnalytics([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 7, 365);
        var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var events = await db.AnalyticsEvents.AsNoTracking()
            .Where(x => x.OccurredAtUtc >= start)
            .Select(x => new { x.EventType, x.Path, x.SessionId, x.Referrer, x.DeviceType, x.OccurredAtUtc })
            .ToListAsync(cancellationToken);

        var dateSeries = Enumerable.Range(0, days)
            .Select(i => start.AddDays(i))
            .Select(day => new ChartPointDto(day.ToString("dd MMM"),
                events.Count(x => x.EventType == "page_view" && x.OccurredAtUtc.Date == day)))
            .ToList();
        var topPages = events.Where(x => x.EventType == "page_view")
            .GroupBy(x => x.Path).OrderByDescending(g => g.Count()).Take(10)
            .Select(g => new ChartPointDto(g.Key, g.Count())).ToList();
        var sources = events.GroupBy(x => string.IsNullOrWhiteSpace(x.Referrer) ? "Direct" :
                Uri.TryCreate(x.Referrer, UriKind.Absolute, out var uri) ? uri.Host : "Other")
            .OrderByDescending(g => g.Count()).Take(8)
            .Select(g => new ChartPointDto(g.Key, g.Count())).ToList();
        var devices = events.GroupBy(x => x.DeviceType ?? "unknown")
            .Select(g => new ChartPointDto(g.Key, g.Count())).ToList();
        var breakdown = events.GroupBy(x => x.EventType)
            .OrderByDescending(g => g.Count())
            .Select(g => new ChartPointDto(g.Key, g.Count())).ToList();
        var pageViews = events.Count(x => x.EventType == "page_view");
        var sessions = events.Where(x => x.SessionId != null).Select(x => x.SessionId).Distinct().Count();
        var checkouts = events.Count(x => x.EventType == "begin_checkout");
        var conversion = pageViews == 0 ? 0m : Math.Round(checkouts * 100m / pageViews, 2);

        return new AnalyticsDashboardDto(
            pageViews, sessions,
            events.Count(x => x.EventType == "product_view"),
            events.Count(x => x.EventType == "add_to_cart"),
            checkouts, conversion,
            dateSeries, topPages, sources, devices, breakdown);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Action.Contains(term) || x.EntityName.Contains(term) || (x.UserEmail != null && x.UserEmail.Contains(term)));
        }
        query = query.OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize)
            .Select(x => new AuditLogDto(x.Id, x.Action, x.EntityName, x.EntityId, x.Details, x.UserEmail, x.IpAddress, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return PagedResult<AuditLogDto>.Create(items, request.Page, request.PageSize, total);
    }

    [HttpGet("ping")]
    [Authorize]
    public IActionResult Ping() => Ok(new { ok = true, staff = User.IsStaff() });
}
