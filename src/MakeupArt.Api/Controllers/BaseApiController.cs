using MakeupArt.Application.Common;
using MakeupArt.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MakeupArt.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid? CurrentUserId => User.GetUserId();

    protected Guid RequireUserId()
        => CurrentUserId ?? throw ApiException.Unauthorized("Please sign in to continue.");

    protected static string AbsoluteUrl(HttpRequest request, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        return $"{request.Scheme}://{request.Host}{path}";
    }
}
