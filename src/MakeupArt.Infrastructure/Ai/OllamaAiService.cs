using System.Net.Http.Json;
using System.Text.Json;
using MakeupArt.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MakeupArt.Infrastructure.Ai;

public record AiMessage(string Role, string Content);
public record AiResult(string Content, bool UsedFallback, string Model);

public interface IAiService
{
    Task<AiResult> ChatAsync(string systemPrompt, IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default);
    Task<AiResult> GenerateAsync(string task, string prompt, string tone, CancellationToken cancellationToken = default);
}

public class OllamaAiService(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    ILogger<OllamaAiService> logger) : IAiService
{
    private readonly AiOptions _options = options.Value;

    public async Task<AiResult> ChatAsync(string systemPrompt, IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return Fallback("The beauty advisor is taking a short break. Browse our services and products, or send us a message — our artists will help you personally.");
        var payloadMessages = new List<object> { new { role = "system", content = systemPrompt } };
        payloadMessages.AddRange(messages.TakeLast(12).Select(m => new { role = m.Role == "assistant" ? "assistant" : "user", content = m.Content }));
        return await SendAsync(payloadMessages, cancellationToken);
    }

    public async Task<AiResult> GenerateAsync(string task, string prompt, string tone, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return Fallback(FallbackCopy(task, prompt));
        var system = $"""
            You are the creative content assistant for Makeup Art, a premium makeup studio in Bengaluru.
            Write in a {tone} tone. Be specific, inclusive, and never make medical claims.
            Return only the requested marketing copy with no preamble or markdown fences.
            Never invent prices, certifications, or availability.
            Task: {task}
            """;
        return await SendAsync([new { role = "system", content = system }, new { role = "user", content = prompt }], cancellationToken);
    }

    private async Task<AiResult> SendAsync(List<object> messages, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 240)));
            var client = httpClientFactory.CreateClient("ollama");
            var payload = new
            {
                model = _options.Model,
                messages,
                stream = false,
                think = false,
                options = new { temperature = 0.55, top_p = 0.9, num_predict = 650 }
            };
            using var response = await client.PostAsJsonAsync($"{_options.BaseUrl.TrimEnd('/')}/api/chat", payload, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama returned {Status}", response.StatusCode);
                return Fallback(FallbackCopy("assistant", string.Empty));
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            var content = json.RootElement.TryGetProperty("message", out var message)
                          && message.TryGetProperty("content", out var contentElement)
                ? contentElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(content))
                return Fallback(FallbackCopy("assistant", string.Empty));
            return new AiResult(content.Trim(), false, _options.Model);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogInformation(ex, "Local AI unavailable; using graceful fallback.");
            return Fallback(FallbackCopy("assistant", string.Empty));
        }
    }

    private AiResult Fallback(string content) => new(content, true, _options.Model);

    private static string FallbackCopy(string task, string prompt) => task switch
    {
        "product_description" => "A considered everyday essential from Makeup Art — buildable colour, a comfortable finish, and a shade story made for real skin. Pair it with our artists' finishing ritual for a look that lasts from the first coffee to the last dance.",
        "seo" => "Discover Makeup Art — bridal artistry, editorial looks, personalised lessons and curated beauty essentials in Bengaluru. Book a consultation with our artists today.",
        "social" => "Soft focus, strong finish. ✨ Our artists are sharing the details behind this season's most requested glow. Book your chair and let's make it yours. #MakeupArt #SoftGlam",
        "insight" => "Revenue and order trends are available in the dashboard. Focus on the strongest-selling products, reorder low-stock essentials, and follow up on pending bookings and support tickets this week.",
        _ => $"Our studio would love to help with that. Share your occasion, skin goals and preferred date, and an artist will guide you personally. ({prompt[..Math.Min(prompt.Length, 80)]})"
    };
}
