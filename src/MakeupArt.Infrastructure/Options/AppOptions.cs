namespace MakeupArt.Infrastructure.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "MakeupArtApi";
    public string Audience { get; set; } = "MakeupArtClient";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}

public class SeedOptions
{
    public string AdminEmail { get; set; } = "admin@makeupart.local";
    public string AdminPassword { get; set; } = "";
    public string CustomerEmail { get; set; } = "priya@example.com";
    public string CustomerPassword { get; set; } = "";
    public bool DemoData { get; set; } = true;
    public bool DemoOrders { get; set; } = true;
}

public class PaymentOptions
{
    public string DefaultProvider { get; set; } = "Mock";
}

public class RazorpayOptions
{
    public string KeyId { get; set; } = "";
    public string KeySecret { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(KeySecret);
}

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "hello@makeupart.local";
    public string FromName { get; set; } = "Makeup Art";
    public bool Enabled => !string.IsNullOrWhiteSpace(Host);
}

public class AiOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen3.5:latest";
    public int TimeoutSeconds { get; set; } = 90;
}

public class BackupOptions
{
    public string ContainerPath { get; set; } = "/var/opt/mssql/backups";
    public string HostPath { get; set; } = "";
    public int RetentionDays { get; set; } = 14;
}

public class FrontendOptions
{
    public string Origin { get; set; } = "http://localhost:4200";
}
