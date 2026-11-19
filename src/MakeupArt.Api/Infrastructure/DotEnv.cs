namespace MakeupArt.Api.Infrastructure;

/// <summary>
/// Minimal .env loader so local development secrets are not stored in committed JSON.
/// Existing process environment variables always win.
/// </summary>
public static class DotEnv
{
    public static void Load()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env")
        };
        var file = candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
        if (file is null) return;

        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase)) line = line[7..].Trim();
            var index = line.IndexOf('=');
            if (index <= 0) continue;
            var key = line[..index].Trim();
            var value = line[(index + 1)..].Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                value = value[1..^1];
            if (string.IsNullOrWhiteSpace(key) || Environment.GetEnvironmentVariable(key) is not null) continue;
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
