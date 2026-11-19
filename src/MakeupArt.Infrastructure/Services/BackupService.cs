using System.Data;
using System.Data.Common;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Content;
using MakeupArt.Infrastructure.Options;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MakeupArt.Infrastructure.Services;

public interface IBackupService
{
    Task<BackupRecord> CreateBackupAsync(string backupType, string? label = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRecord>> ListAsync(CancellationToken cancellationToken = default);
    string? ResolveDownloadPath(BackupRecord record);
    Task<int> PruneAsync(CancellationToken cancellationToken = default);
}

public class BackupService(
    AppDbContext db,
    IOptions<BackupOptions> options,
    ILogger<BackupService> logger) : IBackupService
{
    private readonly BackupOptions _options = options.Value;

    public async Task<IReadOnlyList<BackupRecord>> ListAsync(CancellationToken cancellationToken = default)
        => await db.BackupRecords.OrderByDescending(x => x.StartedAtUtc).Take(100).ToListAsync(cancellationToken);

    public async Task<BackupRecord> CreateBackupAsync(string backupType, string? label = null, CancellationToken cancellationToken = default)
    {
        var databaseName = db.Database.GetDbConnection().Database;
        if (string.IsNullOrWhiteSpace(databaseName) || !databaseName.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
            throw new InvalidOperationException("The configured database name is not safe for a backup command.");

        var safeLabel = string.IsNullOrWhiteSpace(label) ? "" : "-" + string.Concat(label.Where(char.IsLetterOrDigit).Take(30));
        var fileName = $"{databaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}{safeLabel}.bak";
        var containerPath = $"{_options.ContainerPath.TrimEnd('/')}/{fileName}";
        var record = new BackupRecord
        {
            FileName = fileName,
            FilePath = containerPath,
            Status = BackupStatus.Started,
            BackupType = backupType,
            Message = "Backup started."
        };
        db.BackupRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection,
                $"BACKUP DATABASE [{databaseName}] TO DISK = N'{containerPath.Replace("'", "''")}' WITH COMPRESSION, CHECKSUM, INIT, STATS = 10;",
                cancellationToken);
            await ExecuteAsync(connection,
                $"RESTORE VERIFYONLY FROM DISK = N'{containerPath.Replace("'", "''")}';",
                cancellationToken);

            record.Status = BackupStatus.Completed;
            record.CompletedAtUtc = DateTime.UtcNow;
            record.Message = "Backup completed and verified.";
            var hostPath = ResolveDownloadPath(record);
            if (!string.IsNullOrWhiteSpace(hostPath) && File.Exists(hostPath))
                record.SizeBytes = new FileInfo(hostPath).Length;
            await db.SaveChangesAsync(cancellationToken);
            await PruneAsync(cancellationToken);
            logger.LogInformation("Database backup {File} completed.", fileName);
            return record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database backup {File} failed.", fileName);
            record.Status = BackupStatus.Failed;
            record.CompletedAtUtc = DateTime.UtcNow;
            record.Message = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await db.SaveChangesAsync(cancellationToken);
            return record;
        }
    }

    public string? ResolveDownloadPath(BackupRecord record)
    {
        if (string.IsNullOrWhiteSpace(_options.HostPath)) return null;
        var root = Path.GetFullPath(_options.HostPath);
        var path = Path.GetFullPath(Path.Combine(root, Path.GetFileName(record.FileName)));
        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path : null;
    }

    public async Task<int> PruneAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.HostPath) || !Directory.Exists(_options.HostPath)) return 0;
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(_options.HostPath, "*.bak"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                    removed++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not prune backup {File}", file);
            }
        }
        if (removed > 0) logger.LogInformation("Pruned {Count} expired database backups.", removed);
        await Task.CompletedTask;
        return removed;
    }

    private static async Task ExecuteAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
