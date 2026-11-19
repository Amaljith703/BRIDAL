using System.Net;
using System.Security.Cryptography;
using System.Text;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Catalog;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Catalog;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers;

[Authorize]
[Route("api/account")]
public class AccountController(AppDbContext db) : BaseApiController
{
    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.IsDefaultShipping).ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return addresses.Select(x => x.ToDto()).ToList();
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> CreateAddress(AddressRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var count = await db.CustomerAddresses.CountAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        var address = new CustomerAddress
        {
            UserId = userId,
            Label = request.Label,
            FullName = request.FullName,
            Phone = request.Phone,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            IsDefaultShipping = request.IsDefaultShipping || count == 0,
            IsDefaultBilling = request.IsDefaultBilling || count == 0
        };
        if (address.IsDefaultShipping)
        {
            var others = await db.CustomerAddresses.Where(x => x.UserId == userId && x.IsDefaultShipping).ToListAsync(cancellationToken);
            foreach (var other in others) other.IsDefaultShipping = false;
        }
        if (address.IsDefaultBilling)
        {
            var others = await db.CustomerAddresses.Where(x => x.UserId == userId && x.IsDefaultBilling).ToListAsync(cancellationToken);
            foreach (var other in others) other.IsDefaultBilling = false;
        }
        db.CustomerAddresses.Add(address);
        await db.SaveChangesAsync(cancellationToken);
        return address.ToDto();
    }

    [HttpPut("addresses/{id:guid}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(Guid id, AddressRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var address = await db.CustomerAddresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
                      ?? throw ApiException.NotFound("Address");
        address.Label = request.Label;
        address.FullName = request.FullName;
        address.Phone = request.Phone;
        address.AddressLine1 = request.AddressLine1;
        address.AddressLine2 = request.AddressLine2;
        address.City = request.City;
        address.State = request.State;
        address.PostalCode = request.PostalCode;
        address.Country = request.Country;
        if (request.IsDefaultShipping)
        {
            var others = await db.CustomerAddresses.Where(x => x.UserId == userId && x.Id != id && x.IsDefaultShipping).ToListAsync(cancellationToken);
            foreach (var other in others) other.IsDefaultShipping = false;
        }
        if (request.IsDefaultBilling)
        {
            var others = await db.CustomerAddresses.Where(x => x.UserId == userId && x.Id != id && x.IsDefaultBilling).ToListAsync(cancellationToken);
            foreach (var other in others) other.IsDefaultBilling = false;
        }
        address.IsDefaultShipping = request.IsDefaultShipping;
        address.IsDefaultBilling = request.IsDefaultBilling;
        await db.SaveChangesAsync(cancellationToken);
        return address.ToDto();
    }

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var address = await db.CustomerAddresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
                      ?? throw ApiException.NotFound("Address");
        address.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetOrders([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var query = db.Orders.AsNoTracking().Where(x => x.UserId == userId).Include(x => x.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.OrderNumber.Contains(request.Search.Trim()));
        query = query.OrderByDescending(x => x.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<OrderSummaryDto>.Create(items.Select(x => x.ToSummaryDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Items).Include(x => x.Payments).Include(x => x.Invoice).Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw ApiException.NotFound("Order");
        return order.ToDto();
    }

    [HttpGet("orders/{id:guid}/invoice")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Items).Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw ApiException.NotFound("Order");
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (order.Invoice is null) throw ApiException.Validation("An invoice is available once the order is paid.");

        var rows = string.Join("", order.Items.Select(i =>
            $"<tr><td style=\"padding:10px;border-bottom:1px solid #eadfd9\">{WebUtility.HtmlEncode(i.Name)}{(string.IsNullOrWhiteSpace(i.VariantName) ? "" : $"<br><small>{WebUtility.HtmlEncode(i.VariantName)}</small>")}</td>" +
            $"<td style=\"padding:10px;border-bottom:1px solid #eadfd9;text-align:center\">{i.Quantity}</td>" +
            $"<td style=\"padding:10px;border-bottom:1px solid #eadfd9;text-align:right\">₹{i.UnitPrice:N2}</td>" +
            $"<td style=\"padding:10px;border-bottom:1px solid #eadfd9;text-align:right\">₹{i.LineTotal:N2}</td></tr>"));

        var html = $$"""
            <!doctype html><html><head><meta charset="utf-8"><title>{{order.Invoice.InvoiceNumber}} · {{order.OrderNumber}}</title>
            <style>
              body { font-family: Georgia, 'Times New Roman', serif; color: #3d2a2f; max-width: 780px; margin: 40px auto; padding: 0 24px; }
              h1, h2 { font-weight: 400; } .muted { color: #7a666b; }
              table { width: 100%; border-collapse: collapse; margin-top: 24px; }
              .totals td { padding: 6px 10px; } .totals tr:last-child { font-weight: 600; border-top: 1px solid #8E4A5B; }
              .badge { display:inline-block; border:1px solid #8E4A5B; color:#8E4A5B; padding:4px 12px; border-radius:999px; font-size:12px; letter-spacing:.12em; text-transform:uppercase; }
              @media print { .no-print { display:none; } body { margin: 0; } }
            </style></head><body>
              <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:24px">
                <div><h1>{{WebUtility.HtmlEncode(settings.BusinessName)}}</h1><p class="muted">{{WebUtility.HtmlEncode(settings.AddressLine1)}}, {{WebUtility.HtmlEncode(settings.City)}} {{settings.PostalCode}}<br>GSTIN: {{WebUtility.HtmlEncode(order.Invoice.GstNumber ?? settings.GstNumber ?? "—")}}</p></div>
                <div style="text-align:right"><span class="badge">Tax invoice</span><h2>{{order.Invoice.InvoiceNumber}}</h2><p class="muted">Order {{order.OrderNumber}}<br>{{order.Invoice.InvoiceDateUtc:dd MMM yyyy}}</p></div>
              </div>
              <p><strong>Billed to</strong><br>{{WebUtility.HtmlEncode(order.CustomerName)}}<br>{{WebUtility.HtmlEncode(order.CustomerEmail)}}<br>{{WebUtility.HtmlEncode(order.CustomerPhone)}}</p>
              <p class="muted">Place of supply: {{WebUtility.HtmlEncode(order.PlaceOfSupply ?? settings.BusinessState)}}</p>
              <table><thead><tr><th style="text-align:left;padding:10px;border-bottom:2px solid #8E4A5B">Item</th><th style="padding:10px;border-bottom:2px solid #8E4A5B">Qty</th><th style="text-align:right;padding:10px;border-bottom:2px solid #8E4A5B">Unit</th><th style="text-align:right;padding:10px;border-bottom:2px solid #8E4A5B">Total</th></tr></thead>
              <tbody>{{rows}}</tbody></table>
              <table class="totals" style="margin-left:auto;width:320px">
                <tr><td>Subtotal</td><td style="text-align:right">₹{{order.SubTotal:N2}}</td></tr>
                <tr><td>Discount</td><td style="text-align:right">−₹{{order.DiscountAmount:N2}}</td></tr>
                <tr><td>Shipping</td><td style="text-align:right">₹{{order.ShippingAmount:N2}}</td></tr>
                <tr><td>Tax</td><td style="text-align:right">₹{{order.TaxAmount:N2}}</td></tr>
                <tr><td>Total paid</td><td style="text-align:right">₹{{order.TotalAmount:N2}}</td></tr>
              </table>
              <p class="muted" style="margin-top:32px">This is a computer-generated invoice. For questions, contact {{WebUtility.HtmlEncode(settings.SupportEmail ?? settings.Email)}}.</p>
              <button class="no-print" onclick="window.print()" style="background:#8E4A5B;color:#fff;border:0;padding:10px 20px;border-radius:999px;cursor:pointer">Print / save as PDF</button>
            </body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetMyReviews(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var reviews = await db.ProductReviews.AsNoTracking().Where(x => x.UserId == userId)
            .Include(x => x.Product).Include(x => x.User)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return reviews.Select(x => x.ToDto(x.Product?.Name ?? "Makeup Art")).ToList();
    }

    [HttpPost("products/{productId:guid}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(Guid productId, ReviewCreateRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive, cancellationToken)
                      ?? throw ApiException.NotFound("Product");

        var existing = await db.ProductReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId, cancellationToken);
        if (existing is not null && !existing.IsDeleted)
            throw ApiException.Conflict("You have already reviewed this product. Edit your existing review instead.");

        var verified = await db.OrderItems.AnyAsync(x =>
            x.ItemType == OrderItemType.Product && x.ItemId == productId &&
            x.Order != null && x.Order.UserId == userId && x.Order.PaymentStatus == PaymentStatus.Captured, cancellationToken);

        if (existing is not null)
        {
            existing.IsDeleted = false;
            existing.DeletedAtUtc = null;
            existing.IsActive = true;
            existing.Rating = request.Rating;
            existing.Title = request.Title.Trim();
            existing.Body = request.Body.Trim();
            existing.Status = ReviewStatus.Pending;
            existing.IsVerifiedPurchase = verified;
            existing.AdminResponse = null;
            existing.CreatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            existing = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = request.Rating,
                Title = request.Title.Trim(),
                Body = request.Body.Trim(),
                Status = ReviewStatus.Pending,
                IsVerifiedPurchase = verified
            };
            db.ProductReviews.Add(existing);
        }
        await db.SaveChangesAsync(cancellationToken);
        existing.Product = product;
        existing.User = await db.Users.FindAsync([userId], cancellationToken);
        return existing.ToDto(product.Name);
    }

    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var review = await db.ProductReviews.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
                     ?? throw ApiException.NotFound("Review");
        review.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("support/tickets")]
    public async Task<ActionResult<PagedResult<SupportTicketSummaryDto>>> GetTickets([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var query = db.SupportTickets.AsNoTracking().Where(x => x.UserId == userId).Include(x => x.Messages).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Subject.Contains(term) || x.TicketNumber.Contains(term));
        }
        query = query.OrderByDescending(x => x.LastMessageAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<SupportTicketSummaryDto>.Create(items.Select(x => x.ToSummaryDto()).ToList(), request.Page, request.PageSize, total);
    }

    [HttpPost("support/tickets")]
    public async Task<ActionResult<SupportTicketDto>> CreateTicket(SupportTicketCreateRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var user = await db.Users.FindAsync([userId], cancellationToken) ?? throw ApiException.Unauthorized();
        var ticket = new SupportTicket
        {
            TicketNumber = $"TKT-{DateTime.UtcNow:yyyy}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
            UserId = userId,
            Subject = request.Subject.Trim(),
            Category = request.Category.Trim(),
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            LastMessageAtUtc = DateTime.UtcNow,
            Messages =
            [
                new SupportMessage
                {
                    SenderUserId = userId,
                    SenderName = user.DisplayName,
                    SenderType = SenderType.Customer,
                    Body = request.Message.Trim()
                }
            ]
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
        ticket.User = user;
        return ticket.ToDto();
    }

    [HttpGet("support/tickets/{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var ticket = await db.SupportTickets.AsNoTracking()
            .Include(x => x.Messages).Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw ApiException.NotFound("Support ticket");
        ticket.Messages = ticket.Messages.Where(x => x.IsActive && !x.IsInternal).ToList();
        return ticket.ToDto();
    }

    [HttpPost("support/tickets/{id:guid}/messages")]
    public async Task<ActionResult<SupportTicketDto>> AddTicketMessage(Guid id, SupportReplyRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var user = await db.Users.FindAsync([userId], cancellationToken) ?? throw ApiException.Unauthorized();
        var ticket = await db.SupportTickets.Include(x => x.Messages).Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw ApiException.NotFound("Support ticket");
        if (ticket.Status is TicketStatus.Closed or TicketStatus.Resolved)
            ticket.Status = TicketStatus.Open;
        ticket.LastMessageAtUtc = DateTime.UtcNow;
        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderUserId = userId,
            SenderName = user.DisplayName,
            SenderType = SenderType.Customer,
            Body = request.Message.Trim()
        };
        ticket.Messages.Add(message);
        db.SupportMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        ticket.Messages = ticket.Messages.Where(x => x.IsActive && !x.IsInternal).ToList();
        return ticket.ToDto();
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetBookings(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var bookings = await db.BookingRequests.AsNoTracking()
            .Include(x => x.Service).Include(x => x.Package)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return bookings.Select(x => x.ToDto()).ToList();
    }
}
