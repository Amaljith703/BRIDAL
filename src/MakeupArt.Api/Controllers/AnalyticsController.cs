using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Dtos.Commerce;
using MakeupArt.Domain.Content;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MakeupArt.Api.Controllers;

[Route("api/analytics")]
public class AnalyticsController(AppDbContext db) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("events")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Track(AnalyticsEventRequest request, CancellationToken cancellationToken)
    {
        var path = request.Path.Length > 500 ? request.Path[..500] : request.Path;
        if (!path.StartsWith('/')) path = "/" + path;
        var device = request.DeviceType;
        if (string.IsNullOrWhiteSpace(device))
        {
            var agent = Request.Headers.UserAgent.ToString();
            device = agent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ? "mobile"
                : agent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ? "tablet"
                : "desktop";
        }
        var analyticsEvent = new AnalyticsEvent
        {
            EventType = request.EventType.Trim().ToLowerInvariant(),
            Path = path,
            SessionId = request.SessionId?.Length > 80 ? request.SessionId[..80] : request.SessionId,
            UserId = CurrentUserId,
            Referrer = request.Referrer?.Length > 500 ? request.Referrer[..500] : request.Referrer,
            DeviceType = device,
            MetadataJson = request.MetadataJson?.Length > 4000 ? request.MetadataJson[..4000] : request.MetadataJson,
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AnalyticsEvents.Add(analyticsEvent);
        await db.SaveChangesAsync(cancellationToken);
        return Accepted();
    }
}
