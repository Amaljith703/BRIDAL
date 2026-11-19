using MakeupArt.Application.Common;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Content;
using MakeupArt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace MakeupArt.Infrastructure.Services;

public interface IMediaStorageService
{
    Task<MediaAsset> SaveAsync(Stream stream, string originalFileName, string contentType, string folder, string? altText, string? credit, CancellationToken cancellationToken = default);
    Task<MediaAsset> SaveBytesAsync(byte[] bytes, string originalFileName, string contentType, string folder, string? altText, string? credit, CancellationToken cancellationToken = default);
    void DeletePhysicalFile(MediaAsset asset);
}

public class MediaStorageService(
    AppDbContext db,
    IWebHostEnvironment environment,
    ILogger<MediaStorageService> logger) : IMediaStorageService
{
    private static readonly HashSet<string> AllowedImages = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif" };
    private static readonly HashSet<string> AllowedAudio = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".ogg", ".m4a" };
    private static readonly HashSet<string> AllowedVideo = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".mov" };
    private static readonly HashSet<string> AllowedDocuments = new(StringComparer.OrdinalIgnoreCase) { ".pdf" };
    private const long MaxImageBytes = 12 * 1024 * 1024;
    private const long MaxAudioBytes = 25 * 1024 * 1024;
    private const long MaxVideoBytes = 150 * 1024 * 1024;
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    public Task<MediaAsset> SaveAsync(Stream stream, string originalFileName, string contentType, string folder, string? altText, string? credit, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return SaveBytesAsync(memory.ToArray(), originalFileName, contentType, folder, altText, credit, cancellationToken);
    }

    public async Task<MediaAsset> SaveBytesAsync(byte[] bytes, string originalFileName, string contentType, string folder, string? altText, string? credit, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var mediaType = ResolveType(extension);
        var max = mediaType switch
        {
            MediaType.Image => MaxImageBytes,
            MediaType.Audio => MaxAudioBytes,
            MediaType.Video => MaxVideoBytes,
            MediaType.Document => MaxDocumentBytes,
            _ => MaxDocumentBytes
        };
        if (bytes.Length == 0) throw ApiException.Validation("The uploaded file is empty.");
        if (bytes.Length > max) throw ApiException.Validation($"The uploaded file exceeds the {max / (1024 * 1024)} MB limit.");
        if (mediaType == MediaType.Image)
        {
            try
            {
                using var image = SKBitmap.Decode(bytes);
                if (image is null || image.Width <= 0 || image.Height <= 0)
                    throw ApiException.Validation("The uploaded image could not be validated.");
            }
            catch (Exception ex) when (ex is not ApiException)
            {
                throw ApiException.Validation("The uploaded image could not be validated.");
            }
        }
        else if (mediaType == MediaType.Document && !AllowedDocuments.Contains(extension))
        {
            throw ApiException.Validation("Only validated image, audio, video or PDF files can be uploaded.");
        }

        var safeFolder = string.IsNullOrWhiteSpace(folder)
            ? "general"
            : string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(safeFolder)) safeFolder = "general";
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var relative = Path.Combine("media", "uploads", safeFolder, fileName).Replace('\\', '/');
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var physicalDirectory = Path.Combine(webRoot, "media", "uploads", safeFolder);
        Directory.CreateDirectory(physicalDirectory);
        var physicalPath = Path.Combine(physicalDirectory, fileName);
        await File.WriteAllBytesAsync(physicalPath, bytes, cancellationToken);

        var asset = new MediaAsset
        {
            FileName = fileName,
            OriginalFileName = Path.GetFileName(originalFileName),
            Url = "/" + relative,
            MediaType = mediaType,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = bytes.LongLength,
            AltText = altText,
            Credit = credit,
            Folder = safeFolder,
            IsPublic = true
        };
        if (mediaType == MediaType.Image)
        {
            try
            {
                using var image = SKBitmap.Decode(bytes);
                if (image is null) throw new InvalidDataException("Image could not be decoded.");
                asset.Width = image.Width;
                asset.Height = image.Height;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Image metadata could not be read for {File}", fileName);
            }
        }
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public void DeletePhysicalFile(MediaAsset asset)
    {
        try
        {
            var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
                ? Path.Combine(environment.ContentRootPath, "wwwroot")
                : environment.WebRootPath;
            var relative = asset.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(webRoot, relative));
            if (path.StartsWith(Path.GetFullPath(webRoot), StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not remove media file {Url}", asset.Url);
        }
    }

    private static MediaType ResolveType(string extension)
    {
        if (AllowedImages.Contains(extension)) return MediaType.Image;
        if (AllowedAudio.Contains(extension)) return MediaType.Audio;
        if (AllowedVideo.Contains(extension)) return MediaType.Video;
        if (AllowedDocuments.Contains(extension)) return MediaType.Document;
        throw ApiException.Validation("Unsupported file type. Use JPG, PNG, WebP, GIF, MP3, WAV, OGG, MP4, WebM or PDF.");
    }
}
