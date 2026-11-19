using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Commerce;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers;

[Route("api/contact")]
public class ContactController(AppDbContext db, IEmailService emailService) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Send(ContactRequest request, CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync(cancellationToken);
        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = ContactStatus.New
        };
        db.ContactMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(message.Email, "We received your note · Makeup Art",
            $"""
             <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
               <h1 style="font-weight:400">Thank you, {message.Name.Split(' ')[0]}.</h1>
               <p>Your note is with our studio team. We reply to every message personally, usually within one business day.</p>
               <blockquote style="border-left:2px solid #C9A26B;padding-left:16px;color:#7a666b">{System.Net.WebUtility.HtmlEncode(message.Message)}</blockquote>
             </div>
             """, cancellationToken);

        await emailService.SendAsync(settings.Email, $"Website contact · {message.Subject}",
            $"<p><strong>{message.Name}</strong> &lt;{message.Email}&gt; {message.Phone}</p><p>{System.Net.WebUtility.HtmlEncode(message.Message)}</p>",
            cancellationToken);

        return Ok(new { message = "Thank you — your note is with the studio. We will reply shortly." });
    }
}
