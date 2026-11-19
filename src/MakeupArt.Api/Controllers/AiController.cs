using System.Text;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Infrastructure.Ai;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Controllers;

[Route("api/ai")]
public class AiController(AppDbContext db, IAiService ai) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("beauty-advisor")]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<AiChatResponse>> BeautyAdvisor(AiChatRequest request, CancellationToken cancellationToken)
    {
        if (request.Messages.Count == 0) return BadRequest();
        var products = await db.Products.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsFeatured).ThenBy(x => x.SortOrder)
            .Take(20)
            .Select(x => new { x.Id, x.Name, x.ShortDescription, x.Price, x.TagsCsv })
            .ToListAsync(cancellationToken);
        var services = await db.Services.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Take(10)
            .Select(x => new { x.Name, x.ShortDescription, x.Price, x.DurationMinutes })
            .ToListAsync(cancellationToken);

        var catalogue = new StringBuilder();
        foreach (var product in products)
            catalogue.AppendLine($"- PRODUCT: {product.Name} | ₹{product.Price:N0} | {product.ShortDescription} | tags: {product.TagsCsv}");
        foreach (var service in services)
            catalogue.AppendLine($"- SERVICE: {service.Name} | ₹{service.Price:N0} | {service.DurationMinutes} min | {service.ShortDescription}");

        var systemPrompt = $"""
            You are the Makeup Art beauty advisor: warm, practical, inclusive and never pushy.
            You help clients choose services, packages, shades and products for their occasion, skin type and budget.
            Ask at most one clarifying question when needed. Never claim to diagnose skin conditions; recommend a dermatologist for medical concerns.
            Recommend only items from the catalogue below, quoting their exact INR prices when relevant.
            Keep answers under 180 words and use short paragraphs or a compact list.
            Finish with a gentle invitation to book a consultation or ask another question.

            CATALOGUE
            {catalogue}
            """;

        var result = await ai.ChatAsync(systemPrompt,
            request.Messages.TakeLast(10).Select(m => new AiMessage(m.Role == "assistant" ? "assistant" : "user", m.Content)).ToList(),
            cancellationToken);

        var mentioned = products
            .Where(p => result.Content.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Id)
            .Take(4)
            .ToList();
        if (mentioned.Count == 0)
        {
            var last = request.Messages.LastOrDefault()?.Content.ToLowerInvariant() ?? "";
            mentioned = products
                .Where(p => !string.IsNullOrWhiteSpace(p.TagsCsv) &&
                            p.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(tag => last.Contains(tag.Trim().ToLowerInvariant())))
                .Select(p => p.Id).Take(4).ToList();
        }
        return new AiChatResponse(result.Content, result.UsedFallback, result.Model, mentioned);
    }

    [Authorize(Policy = "Staff")]
    [HttpPost("assist")]
    public async Task<ActionResult<AiAssistResponse>> Assist(AiAssistRequest request, CancellationToken cancellationToken)
    {
        var result = await ai.GenerateAsync(request.Task, request.Prompt, string.IsNullOrWhiteSpace(request.Tone) ? "warm, premium, concise" : request.Tone, cancellationToken);
        return new AiAssistResponse(result.Content, result.UsedFallback, result.Model);
    }
}
