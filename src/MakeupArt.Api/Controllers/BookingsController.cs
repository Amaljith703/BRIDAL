using System.Security.Cryptography;
using System.Text;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers;

[Route("api/bookings")]
public class BookingsController(AppDbContext db, IEmailService emailService) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<BookingDto>> CreateBooking(BookingCreateRequest request, CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (request.ServiceId is null && request.PackageId is null)
            throw ApiException.Validation("Choose a service or package for your appointment.");
        if (request.PreferredDate.Date < DateTime.UtcNow.Date)
            throw ApiException.Validation("Please choose today or a future date.");
        if (request.PreferredDate.Date > DateTime.UtcNow.Date.AddMonths(18))
            throw ApiException.Validation("Bookings are open up to 18 months ahead.");

        Domain.Catalog.Service? service = null;
        Domain.Catalog.ServicePackage? package = null;
        if (request.ServiceId is not null)
        {
            service = await db.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.IsActive, cancellationToken)
                      ?? throw ApiException.NotFound("Service");
        }
        if (request.PackageId is not null)
        {
            package = await db.ServicePackages.FirstOrDefaultAsync(x => x.Id == request.PackageId && x.IsActive, cancellationToken)
                      ?? throw ApiException.NotFound("Package");
        }

        var booking = new BookingRequest
        {
            BookingNumber = $"BK-{DateTime.UtcNow:yyyy}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
            UserId = CurrentUserId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            ServiceId = service?.Id,
            PackageId = package?.Id,
            PreferredDate = request.PreferredDate.Date,
            PreferredTime = request.PreferredTime,
            Occasion = request.Occasion,
            Notes = request.Notes,
            Status = BookingStatus.Pending
        };
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        var item = package?.Name ?? service?.Name ?? "studio appointment";
        await emailService.SendAsync(booking.Email, $"Your Makeup Art booking request · {booking.BookingNumber}",
            $"""
             <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
               <h1 style="font-weight:400">We have your request.</h1>
               <p>Thank you, {booking.FullName.Split(' ')[0]}. We have asked the studio to hold <strong>{item}</strong> on <strong>{booking.PreferredDate:dd MMMM yyyy}</strong> at {booking.PreferredTime}.</p>
               <p>Your reference is <strong>{booking.BookingNumber}</strong>. An artist will confirm availability and any deposit within one business day.</p>
               <p style="font-size:13px;color:#7a666b">Need to change something? Reply to this email or call {settings.Phone}.</p>
             </div>
             """, cancellationToken);

        await emailService.SendAsync(settings.Email, $"New booking request · {booking.BookingNumber}",
            $"<p>{booking.FullName} requested {item} on {booking.PreferredDate:dd MMM yyyy} at {booking.PreferredTime}.</p>" +
            $"<p>Phone: {booking.Phone}<br>Email: {booking.Email}<br>Occasion: {booking.Occasion}<br>Notes: {booking.Notes}</p>",
            cancellationToken);

        booking.Service = service;
        booking.Package = package;
        return booking.ToDto();
    }

    [AllowAnonymous]
    [HttpGet("{bookingNumber}")]
    public async Task<ActionResult<BookingDto>> GetBooking(string bookingNumber, [FromQuery] string email, CancellationToken cancellationToken)
    {
        var booking = await db.BookingRequests.AsNoTracking()
            .Include(x => x.Service).Include(x => x.Package)
            .FirstOrDefaultAsync(x => x.BookingNumber == bookingNumber && x.Email == email.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw ApiException.NotFound("Booking");
        return booking.ToDto();
    }
}
