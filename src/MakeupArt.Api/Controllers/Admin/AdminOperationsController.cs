using System.Security.Cryptography;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Identity;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers.Admin;

[Authorize(Policy = "Staff")]
[Route("api/admin")]
public class AdminOperationsController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    ILogger<AdminOperationsController> logger) : BaseApiController
{
    private static readonly string[] StaffRoles = ["Admin", "Manager", "Support", "Customer"];

    // ── Orders ─────────────────────────────────────────────────────────────
    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetOrders(
        [FromQuery] PagedRequest request,
        [FromQuery] OrderStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (paymentStatus is not null) query = query.Where(x => x.PaymentStatus == paymentStatus);
        if (from is not null) query = query.Where(x => x.CreatedAtUtc >= from.Value.Date);
        if (to is not null) query = query.Where(x => x.CreatedAtUtc < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(term) || x.CustomerName.Contains(term) || x.CustomerEmail.Contains(term));
        }
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "total" => request.IsAscending ? query.OrderBy(x => x.TotalAmount) : query.OrderByDescending(x => x.TotalAmount),
            "number" => request.IsAscending ? query.OrderBy(x => x.OrderNumber) : query.OrderByDescending(x => x.OrderNumber),
            "status" => query.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<OrderSummaryDto>.Create(items.Select(x => x.ToSummaryDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<AdminOrderDto>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, asTracking: false, cancellationToken);
        return MapOrder(order);
    }

    [HttpPut("orders/{id:guid}/status")]
    public async Task<ActionResult<AdminOrderDto>> UpdateOrderStatus(Guid id, OrderStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, asTracking: true, cancellationToken);
        var previous = order.Status;

        if (previous is OrderStatus.Cancelled or OrderStatus.Refunded && request.Status != previous)
            throw ApiException.Validation("A cancelled or refunded order cannot be reopened. Create a new order instead.");
        if (request.Status == OrderStatus.Shipped && string.IsNullOrWhiteSpace(request.TrackingNumber) && string.IsNullOrWhiteSpace(order.TrackingNumber))
            throw ApiException.Validation("Add a tracking number before marking this order as shipped.");
        if (!string.IsNullOrWhiteSpace(request.TrackingNumber)) order.TrackingNumber = request.TrackingNumber.Trim();
        if (request.Status == OrderStatus.Cancelled) order.CancelledAtUtc = DateTime.UtcNow;

        order.Status = request.Status;
        var history = new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = request.Status,
            Note = string.IsNullOrWhiteSpace(request.Note) ? $"Status changed by {(User.Identity?.Name ?? "staff")}." : request.Note.Trim(),
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow
        };
        db.OrderStatusHistories.Add(history);
        order.StatusHistory.Add(history);
        Audit("order.status_changed", nameof(Order), order.Id.ToString(), $"{previous} → {request.Status} for {order.OrderNumber}.");
        await db.SaveChangesAsync(cancellationToken);

        if (request.Status == OrderStatus.Shipped && !string.IsNullOrWhiteSpace(order.TrackingNumber))
        {
            await emailService.SendAsync(order.CustomerEmail, $"Your Makeup Art order {order.OrderNumber} is on its way",
                $"""
                 <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
                   <h1 style="font-weight:400">A parcel for you.</h1>
                   <p>Order <strong>{order.OrderNumber}</strong> has shipped with {System.Net.WebUtility.HtmlEncode(order.ShippingMethodName)}.</p>
                   <p>Tracking number: <strong>{System.Net.WebUtility.HtmlEncode(order.TrackingNumber)}</strong></p>
                   <p>If anything looks unusual, reply to this email and our care team will help.</p>
                 </div>
                 """, cancellationToken);
        }

        return MapOrder(order);
    }

    [HttpPut("orders/{id:guid}/notes")]
    public async Task<ActionResult<AdminOrderDto>> UpdateOrderNotes(Guid id, AdminNotesRequest request, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, asTracking: true, cancellationToken);
        order.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes) ? null : request.AdminNotes.Trim();
        Audit("order.notes_updated", nameof(Order), order.Id.ToString(), $"Admin notes updated for {order.OrderNumber}.");
        await db.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("orders/{id:guid}/refund")]
    public async Task<ActionResult<AdminOrderDto>> CreateRefund(Guid id, RefundRequest request, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAsync(id, asTracking: true, cancellationToken);
        var payment = order.Payments
            .Where(x => x.Status is PaymentStatus.Captured or PaymentStatus.PartiallyRefunded)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault()
            ?? throw ApiException.Validation("There is no captured payment to refund on this order.");
        var refundable = Math.Round(order.TotalAmount - order.Refunds.Where(x => x.Status != RefundStatus.Failed).Sum(x => x.Amount), 2);
        var amount = Math.Round(request.Amount, 2);
        if (amount <= 0) throw ApiException.Validation("Enter a refund amount greater than zero.");
        if (amount > refundable) throw ApiException.Validation($"Only ₹{refundable:N2} remains refundable on this order.");

        var isMock = payment.Provider == PaymentProviderType.Mock;
        var refund = new Refund
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            Amount = amount,
            Reason = request.Reason.Trim(),
            Status = isMock ? RefundStatus.Processed : RefundStatus.Pending,
            ProviderRefundId = isMock ? $"mock_refund_{RandomNumberGenerator.GetHexString(10).ToLowerInvariant()}" : null,
            ProcessedAtUtc = isMock ? DateTime.UtcNow : null,
            ProcessedByUserId = isMock ? CurrentUserId : null
        };
        db.Refunds.Add(refund);
        order.Refunds.Add(refund);
        ApplyRefundState(order, $"Refund of ₹{amount:N2} created by {(User.Identity?.Name ?? "staff")}.", CurrentUserId);
        Audit("order.refund_created", nameof(Order), order.Id.ToString(), $"₹{amount:N2} refund for {order.OrderNumber}: {request.Reason.Trim()}");
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Refund {RefundId} created for order {OrderNumber}", refund.Id, order.OrderNumber);
        return MapOrder(order);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("refunds/{id:guid}/status")]
    public async Task<ActionResult<AdminOrderDto>> UpdateRefundStatus(Guid id, RefundStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var refund = await db.Refunds.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw ApiException.NotFound("Refund");
        var order = await LoadOrderAsync(refund.OrderId, asTracking: true, cancellationToken);
        var tracked = order.Refunds.First(x => x.Id == id);
        if (request.Status == RefundStatus.Processed)
        {
            tracked.Status = RefundStatus.Processed;
            tracked.ProcessedAtUtc = DateTime.UtcNow;
            tracked.ProcessedByUserId = CurrentUserId;
            if (!string.IsNullOrWhiteSpace(request.ProviderRefundId)) tracked.ProviderRefundId = request.ProviderRefundId.Trim();
            ApplyRefundState(order, $"Refund of ₹{tracked.Amount:N2} marked as processed in the payment provider.", CurrentUserId);
        }
        else if (request.Status == RefundStatus.Failed)
        {
            tracked.Status = RefundStatus.Failed;
            ApplyRefundState(order, $"Refund of ₹{tracked.Amount:N2} failed at the payment provider.", CurrentUserId);
        }
        else
        {
            tracked.Status = RefundStatus.Pending;
        }
        Audit("refund.status_changed", nameof(Refund), tracked.Id.ToString(), $"Refund status {request.Status} for {order.OrderNumber}.");
        await db.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    // ── Customers ──────────────────────────────────────────────────────────
    [HttpGet("customers")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetCustomers([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().Where(x => !x.IsDeleted);
        if (request.IsActive is not null) query = query.Where(x => x.IsActive == request.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => (x.Email != null && x.Email.Contains(term))
                || x.DisplayName.Contains(term)
                || (x.PhoneNumber != null && x.PhoneNumber.Contains(term)));
        }

        var projected = query.Select(user => new
        {
            User = user,
            OrderCount = db.Orders.Count(o => o.UserId == user.Id),
            TotalSpend = db.Orders.Where(o => o.UserId == user.Id && o.PaymentStatus == PaymentStatus.Captured)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m
        });
        projected = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.IsAscending ? projected.OrderBy(x => x.User.DisplayName) : projected.OrderByDescending(x => x.User.DisplayName),
            "orders" => request.IsAscending ? projected.OrderBy(x => x.OrderCount) : projected.OrderByDescending(x => x.OrderCount),
            "spend" => request.IsAscending ? projected.OrderBy(x => x.TotalSpend) : projected.OrderByDescending(x => x.TotalSpend),
            _ => projected.OrderByDescending(x => x.User.CreatedAtUtc)
        };

        var total = await projected.CountAsync(cancellationToken);
        var page = await projected.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        var items = new List<AdminUserDto>(page.Count);
        foreach (var row in page)
        {
            var roles = await userManager.GetRolesAsync(row.User);
            items.Add(MapUser(row.User, roles.ToList(), row.OrderCount, row.TotalSpend));
        }
        return PagedResult<AdminUserDto>.Create(items, request.Page, request.PageSize, total);
    }

    [HttpGet("customers/{id:guid}")]
    public async Task<ActionResult<AdminCustomerDetailDto>> GetCustomer(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
                   ?? throw ApiException.NotFound("Customer");
        var roles = await userManager.GetRolesAsync(user);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Items)
            .Where(x => x.UserId == id).OrderByDescending(x => x.CreatedAtUtc).Take(10).ToListAsync(cancellationToken);
        var orderCount = await db.Orders.CountAsync(x => x.UserId == id, cancellationToken);
        var spend = await db.Orders.Where(x => x.UserId == id && x.PaymentStatus == PaymentStatus.Captured)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;
        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(x => x.UserId == id && x.IsActive).OrderByDescending(x => x.IsDefaultShipping)
            .Select(x => new CustomerAddressAdminDto(x.Id, x.Label, x.FullName, x.Phone, x.AddressLine1, x.AddressLine2,
                x.City, x.State, x.PostalCode, x.Country, x.IsDefaultShipping))
            .ToListAsync(cancellationToken);
        return new AdminCustomerDetailDto(
            MapUser(user, roles.ToList(), orderCount, spend),
            user.AdminNotes,
            addresses,
            orders.Select(x => x.ToSummaryDto()).ToList());
    }

    [HttpPut("customers/{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> UpdateCustomer(Guid id, UserAdminUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
                   ?? throw ApiException.NotFound("Customer");
        if (user.Id == CurrentUserId && !request.IsActive)
            throw ApiException.Validation("You cannot deactivate your own account.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim();
            if (!StaffRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                throw ApiException.Validation($"Role must be one of {string.Join(", ", StaffRoles)}.");
            var normalized = StaffRoles.First(x => x.Equals(role, StringComparison.OrdinalIgnoreCase));
            if (currentRoles.Contains("Admin") && normalized != "Admin")
            {
                var admins = await userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count(x => x.IsActive && !x.IsDeleted && x.Id != user.Id) == 0)
                    throw ApiException.Validation("Keep at least one active administrator before changing this account's role.");
            }
            var removable = currentRoles.Where(x => StaffRoles.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
            if (removable.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, removable);
                if (!removeResult.Succeeded) throw ApiException.Validation(string.Join(" ", removeResult.Errors.Select(x => x.Description)));
            }
            var addResult = await userManager.AddToRoleAsync(user, normalized);
            if (!addResult.Succeeded) throw ApiException.Validation(string.Join(" ", addResult.Errors.Select(x => x.Description)));
        }
        else if (!request.IsActive && currentRoles.Contains("Admin"))
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count(x => x.IsActive && !x.IsDeleted && x.Id != user.Id) == 0)
                throw ApiException.Validation("Keep at least one active administrator account.");
        }

        user.IsActive = request.IsActive;
        user.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes) ? null : request.AdminNotes.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) throw ApiException.Validation(string.Join(" ", result.Errors.Select(x => x.Description)));
        Audit("customer.updated", nameof(ApplicationUser), user.Id.ToString(), $"Active: {request.IsActive}. Role: {request.Role ?? "unchanged"}.");
        await db.SaveChangesAsync(cancellationToken);
        var roles = await userManager.GetRolesAsync(user);
        var orderCount = await db.Orders.CountAsync(x => x.UserId == user.Id, cancellationToken);
        var spend = await db.Orders.Where(x => x.UserId == user.Id && x.PaymentStatus == PaymentStatus.Captured)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;
        return MapUser(user, roles.ToList(), orderCount, spend);
    }

    // ── Bookings ───────────────────────────────────────────────────────────
    [HttpGet("bookings")]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetBookings(
        [FromQuery] PagedRequest request,
        [FromQuery] BookingStatus? status,
        [FromQuery] DateTime? from,
        CancellationToken cancellationToken)
    {
        var query = db.BookingRequests.AsNoTracking().Include(x => x.Service).Include(x => x.Package).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (from is not null) query = query.Where(x => x.PreferredDate >= from.Value.Date);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.BookingNumber.Contains(term) || x.FullName.Contains(term) || x.Email.Contains(term) || x.Phone.Contains(term));
        }
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "date" => request.IsAscending ? query.OrderBy(x => x.PreferredDate) : query.OrderByDescending(x => x.PreferredDate),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<BookingDto>.Create(items.Select(x => x.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPut("bookings/{id:guid}")]
    public async Task<ActionResult<BookingDto>> UpdateBooking(Guid id, BookingStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.BookingRequests.Include(x => x.Service).Include(x => x.Package)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Booking");
        var previous = booking.Status;
        booking.Status = request.Status;
        booking.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes) ? booking.AdminNotes : request.AdminNotes.Trim();
        Audit("booking.status_changed", nameof(BookingRequest), booking.Id.ToString(), $"{previous} → {request.Status} for {booking.BookingNumber}.");
        await db.SaveChangesAsync(cancellationToken);

        if (request.Status is BookingStatus.Confirmed or BookingStatus.Cancelled && previous != request.Status)
        {
            var item = booking.Package?.Name ?? booking.Service?.Name ?? "your studio appointment";
            var heading = request.Status == BookingStatus.Confirmed ? "Your chair is confirmed." : "Your appointment was cancelled.";
            await emailService.SendAsync(booking.Email, $"Makeup Art booking {booking.BookingNumber} · {request.Status}",
                $"""
                 <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
                   <h1 style="font-weight:400">{heading}</h1>
                   <p>Hello {System.Net.WebUtility.HtmlEncode(booking.FullName.Split(' ')[0])},</p>
                   <p>{System.Net.WebUtility.HtmlEncode(item)} · <strong>{booking.PreferredDate:dd MMMM yyyy}</strong> at {System.Net.WebUtility.HtmlEncode(booking.PreferredTime)}.</p>
                   {(string.IsNullOrWhiteSpace(booking.AdminNotes) ? "" : $"<p><strong>Studio note:</strong> {System.Net.WebUtility.HtmlEncode(booking.AdminNotes)}</p>")}
                   <p>Reference: <strong>{booking.BookingNumber}</strong></p>
                 </div>
                 """, cancellationToken);
        }
        return booking.ToDto();
    }

    // ── Contact messages ───────────────────────────────────────────────────
    [HttpGet("contacts")]
    public async Task<ActionResult<PagedResult<ContactMessageDto>>> GetContacts(
        [FromQuery] PagedRequest request, [FromQuery] ContactStatus? status, CancellationToken cancellationToken)
    {
        var query = db.ContactMessages.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Email.Contains(term) || x.Subject.Contains(term) || x.Message.Contains(term));
        }
        query = query.OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<ContactMessageDto>.Create(items.Select(MapContact).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPut("contacts/{id:guid}")]
    public async Task<ActionResult<ContactMessageDto>> UpdateContact(Guid id, ContactStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var contact = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                      ?? throw ApiException.NotFound("Contact message");
        contact.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.AdminNotes)) contact.AdminNotes = request.AdminNotes.Trim();
        Audit("contact.status_changed", nameof(ContactMessage), contact.Id.ToString(), $"Status set to {request.Status}.");
        await db.SaveChangesAsync(cancellationToken);
        return MapContact(contact);
    }

    // ── Review moderation ──────────────────────────────────────────────────
    [HttpGet("reviews")]
    public async Task<ActionResult<PagedResult<AdminReviewItemDto>>> GetReviews(
        [FromQuery] PagedRequest request, [FromQuery] ReviewStatus? status, CancellationToken cancellationToken)
    {
        var query = db.ProductReviews.AsNoTracking().Include(x => x.Product).Include(x => x.User).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Body.Contains(term)
                || (x.Product != null && x.Product.Name.Contains(term))
                || (x.User != null && x.User.DisplayName.Contains(term)));
        }
        query = query.OrderBy(x => x.Status == ReviewStatus.Pending ? 0 : 1).ThenByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<AdminReviewItemDto>.Create(items.Select(x => new AdminReviewItemDto(
            x.Id, x.ProductId, x.Product?.Name ?? "Makeup Art", x.User?.DisplayName, x.User?.Email,
            x.Rating, x.Title, x.Body, x.Status.ToString(), x.IsVerifiedPurchase, x.AdminResponse, x.CreatedAtUtc)).ToList(),
            request.Page, request.PageSize, total);
    }

    [HttpPut("reviews/{id:guid}")]
    public async Task<ActionResult<AdminReviewItemDto>> ModerateReview(Guid id, ReviewModerationRequest request, CancellationToken cancellationToken)
    {
        var review = await db.ProductReviews.Include(x => x.Product).Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Review");
        review.Status = request.Status;
        review.AdminResponse = string.IsNullOrWhiteSpace(request.AdminResponse) ? null : request.AdminResponse.Trim();
        if (review.Status != ReviewStatus.Approved) review.IsActive = false;
        Audit("review.moderated", nameof(ProductReview), review.Id.ToString(), $"Review set to {request.Status}.");
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateRatingAsync(review.ProductId, cancellationToken);
        return new AdminReviewItemDto(review.Id, review.ProductId, review.Product?.Name ?? "Makeup Art",
            review.User?.DisplayName, review.User?.Email, review.Rating, review.Title, review.Body,
            review.Status.ToString(), review.IsVerifiedPurchase, review.AdminResponse, review.CreatedAtUtc);
    }

    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var review = await db.ProductReviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw ApiException.NotFound("Review");
        review.IsActive = false;
        Audit("review.deleted", nameof(ProductReview), review.Id.ToString(), "Review removed from the storefront.");
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateRatingAsync(review.ProductId, cancellationToken);
        return NoContent();
    }

    // ── Support tickets ────────────────────────────────────────────────────
    [HttpGet("support/tickets")]
    public async Task<ActionResult<PagedResult<SupportTicketSummaryDto>>> GetTickets(
        [FromQuery] PagedRequest request,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        CancellationToken cancellationToken)
    {
        var query = db.SupportTickets.AsNoTracking().Include(x => x.Messages).Include(x => x.User).AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (priority is not null) query = query.Where(x => x.Priority == priority);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.TicketNumber.Contains(term) || x.Subject.Contains(term)
                || (x.User != null && (x.User.DisplayName.Contains(term) || (x.User.Email != null && x.User.Email.Contains(term)))));
        }
        query = query.OrderBy(x => x.Status == TicketStatus.Open ? 0 : 1)
            .ThenByDescending(x => x.Priority).ThenByDescending(x => x.LastMessageAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<SupportTicketSummaryDto>.Create(items.Select(x => x.ToSummaryDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("support/tickets/{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await db.SupportTickets.AsNoTracking().Include(x => x.Messages).Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Support ticket");
        return ticket.ToDto();
    }

    [HttpPut("support/tickets/{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> UpdateTicket(Guid id, SupportTicketUpdateRequest request, CancellationToken cancellationToken)
    {
        var ticket = await db.SupportTickets.Include(x => x.Messages).Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Support ticket");
        ticket.Status = request.Status;
        if (request.Priority is not null) ticket.Priority = request.Priority.Value;
        Audit("support.status_changed", nameof(SupportTicket), ticket.Id.ToString(), $"{ticket.TicketNumber}: {request.Status}.");
        await db.SaveChangesAsync(cancellationToken);
        return ticket.ToDto();
    }

    [HttpPost("support/tickets/{id:guid}/reply")]
    public async Task<ActionResult<SupportTicketDto>> ReplyToTicket(Guid id, SupportReplyRequest request, CancellationToken cancellationToken)
    {
        var ticket = await db.SupportTickets.Include(x => x.Messages).Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Support ticket");
        if (ticket.Status is TicketStatus.Closed) ticket.Status = TicketStatus.Open;
        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderUserId = CurrentUserId,
            SenderName = User.Identity?.Name ?? "Makeup Art support",
            SenderType = SenderType.Support,
            Body = request.Message.Trim(),
            IsInternal = request.IsInternal
        };
        ticket.Messages.Add(message);
        db.SupportMessages.Add(message);
        ticket.LastMessageAtUtc = DateTime.UtcNow;
        if (!request.IsInternal) ticket.Status = TicketStatus.Pending;
        Audit("support.replied", nameof(SupportTicket), ticket.Id.ToString(), $"{ticket.TicketNumber}: {(request.IsInternal ? "internal" : "customer")} reply.");
        await db.SaveChangesAsync(cancellationToken);

        if (!request.IsInternal && ticket.User?.Email is { Length: > 0 } email)
        {
            await emailService.SendAsync(email, $"Re: {ticket.Subject} · Makeup Art support",
                $"""
                 <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
                   <p>Hello {System.Net.WebUtility.HtmlEncode(ticket.User.DisplayName.Split(' ')[0])},</p>
                   <p>Our team has replied to your support request <strong>{ticket.TicketNumber}</strong>:</p>
                   <blockquote style="border-left:2px solid #C9A26B;padding-left:16px;color:#544247">{System.Net.WebUtility.HtmlEncode(message.Body)}</blockquote>
                   <p>You can reply from your account at any time.</p>
                 </div>
                 """, cancellationToken);
        }
        return ticket.ToDto();
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private async Task<Order> LoadOrderAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        var query = asTracking ? db.Orders.AsQueryable() : db.Orders.AsNoTracking();
        return await query
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Include(x => x.Refunds)
            .Include(x => x.Invoice)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("Order");
    }

    private static AdminOrderDto MapOrder(Order x) => new(
        x.Id, x.OrderNumber, x.CustomerName, x.CustomerEmail, x.CustomerPhone,
        x.Status.ToString(), x.PaymentStatus.ToString(), x.SubTotal, x.DiscountAmount, x.ShippingAmount,
        x.TaxAmount, x.TotalAmount, x.Currency, x.CouponCode, x.Notes, x.AdminNotes,
        x.ShippingMethodName, x.TrackingNumber, x.GstNumber, x.PlaceOfSupply, x.IsGstInvoice,
        x.PlacedAtUtc, x.PaidAtUtc, x.CreatedAtUtc, x.ShippingAddressJson, x.BillingAddressJson,
        x.Items.Select(i => i.ToDto()).ToList(),
        x.Payments.Select(p => p.ToDto()).ToList(),
        x.Refunds.OrderByDescending(r => r.CreatedAtUtc).Select(r => new AdminRefundDto(
            r.Id, r.Amount, r.Reason, r.Status.ToString(), r.ProviderRefundId, r.ProcessedAtUtc, r.CreatedAtUtc)).ToList(),
        x.Invoice?.InvoiceNumber,
        x.StatusHistory.OrderBy(h => h.ChangedAtUtc).Select(h => new OrderStatusEventDto(h.Status, h.Note, h.ChangedAtUtc)).ToList());

    private static AdminUserDto MapUser(ApplicationUser user, IReadOnlyList<string> roles, int orderCount, decimal totalSpend)
        => new(user.Id, user.Email ?? "", user.DisplayName, user.PhoneNumber, user.IsActive, user.EmailConfirmed,
            user.TwoFactorEnabled, user.CreatedAtUtc, user.LastLoginAtUtc, roles, orderCount, totalSpend);

    private static ContactMessageDto MapContact(ContactMessage x)
        => new(x.Id, x.Name, x.Email, x.Phone, x.Subject, x.Message, x.Status.ToString(), x.AdminNotes, x.CreatedAtUtc);

    private void ApplyRefundState(Order order, string note, Guid? staffId)
    {
        var refunded = Math.Round(order.Refunds.Where(x => x.Status == RefundStatus.Processed).Sum(x => x.Amount), 2);
        var fullyRefunded = refunded >= order.TotalAmount;
        var previous = order.Status;
        if (refunded > 0)
        {
            order.PaymentStatus = fullyRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            if (fullyRefunded) order.Status = OrderStatus.Refunded;
            foreach (var payment in order.Payments)
            {
                var paymentRefunds = order.Refunds.Where(x => x.PaymentId == payment.Id && x.Status == RefundStatus.Processed).Sum(x => x.Amount);
                if (paymentRefunds <= 0) continue;
                payment.Status = paymentRefunds >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            }
        }
        else if (order.Payments.Any(x => x.Status == PaymentStatus.Captured))
        {
            order.PaymentStatus = PaymentStatus.Captured;
        }
        if (order.Status != previous)
        {
            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                Note = note,
                ChangedByUserId = staffId,
                ChangedAtUtc = DateTime.UtcNow
            };
            db.OrderStatusHistories.Add(history);
            order.StatusHistory.Add(history);
        }
    }

    private async Task RecalculateRatingAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
        if (product is null) return;
        var ratings = await db.ProductReviews
            .Where(x => x.ProductId == productId && x.Status == ReviewStatus.Approved && x.IsActive)
            .Select(x => x.Rating).ToListAsync(cancellationToken);
        product.RatingCount = ratings.Count;
        product.RatingAverage = ratings.Count == 0 ? 0d : Math.Round(ratings.Average(), 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void Audit(string action, string entityName, string entityId, string details)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = CurrentUserId,
            UserEmail = User.GetEmail(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details.Length > 1000 ? details[..1000] : details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });
    }
}
