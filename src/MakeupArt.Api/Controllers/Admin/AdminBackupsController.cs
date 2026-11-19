using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Admin;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MakeupArt.Api.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("api/admin/backups")]
public class AdminBackupsController(IBackupService backups) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BackupRecordDto>>> List(CancellationToken cancellationToken)
    {
        var records = await backups.ListAsync(cancellationToken);
        return records.Select(x => new BackupRecordDto(x.Id, x.FileName, x.SizeBytes, x.StartedAtUtc,
            x.CompletedAtUtc, x.Status.ToString(), x.Message, x.BackupType)).ToList();
    }

    [HttpPost]
    [RequestSizeLimit(1024)]
    public async Task<ActionResult<BackupRecordDto>> Create(BackupRequest? request, CancellationToken cancellationToken)
    {
        var record = await backups.CreateBackupAsync("manual", request?.Label, cancellationToken);
        var dto = new BackupRecordDto(record.Id, record.FileName, record.SizeBytes, record.StartedAtUtc,
            record.CompletedAtUtc, record.Status.ToString(), record.Message, record.BackupType);
        return record.Status == MakeupArt.Domain.Common.BackupStatus.Completed
            ? Ok(dto)
            : StatusCode(StatusCodes.Status502BadGateway, dto);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var records = await backups.ListAsync(cancellationToken);
        var record = records.FirstOrDefault(x => x.Id == id) ?? throw ApiException.NotFound("Backup");
        var path = backups.ResolveDownloadPath(record);
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            throw ApiException.NotFound("Backup file");
        return PhysicalFile(path, "application/octet-stream", record.FileName);
    }

    [HttpPost("prune")]
    public async Task<ActionResult<object>> Prune(CancellationToken cancellationToken)
    {
        var removed = await backups.PruneAsync(cancellationToken);
        return Ok(new { removed, message = removed == 0 ? "No expired backups to remove." : $"Removed {removed} expired backup file(s)." });
    }
}
