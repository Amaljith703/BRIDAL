namespace MakeupArt.Application.Common;

public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 12;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 12 : value;
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "desc";
    public bool? IsActive { get; set; }

    public int Skip => (Page - 1) * PageSize;
    public bool IsAscending => string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase);
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}

public class ApiException(string message, int statusCode = 400, string? code = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? Code { get; } = code;

    public static ApiException NotFound(string what = "Resource") => new($"{what} was not found.", 404, "not_found");
    public static ApiException Forbidden(string message = "You do not have permission to perform this action.") => new(message, 403, "forbidden");
    public static ApiException Conflict(string message) => new(message, 409, "conflict");
    public static ApiException Unauthorized(string message = "Authentication is required.") => new(message, 401, "unauthorized");
    public static ApiException Validation(string message) => new(message, 422, "validation_failed");
}
