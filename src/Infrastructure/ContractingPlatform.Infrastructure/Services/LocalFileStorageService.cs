using ContractingPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ContractingPlatform.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<LocalFileStorageService> _logger;

    // Production Construction & Engineering Allowed File Extensions
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".dwg",
        ".dxf",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".doc",
        ".docx"
    };

    // 20 MB max file size for engineering blueprints & high-res site photos
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment, ILogger<LocalFileStorageService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public bool IsAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    public bool IsAllowedFileSize(long sizeInBytes)
    {
        return sizeInBytes > 0 && sizeInBytes <= MaxFileSizeBytes;
    }

    public async Task<UploadedFileResult> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedExtension(originalFileName))
        {
            throw new InvalidOperationException($"امتداد الملف '{Path.GetExtension(originalFileName)}' غير مسموح به. الامتدادات المعتمدة: PDF, DWG, DXF, PNG, JPG, WEBP.");
        }

        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"حجم الملف يتجاوز الحد المسموح به (20 ميجابايت).");
        }

        // Clean & sanitize folder
        var safeFolder = string.IsNullOrWhiteSpace(subFolder) ? "general" : subFolder.Trim().ToLowerInvariant();
        safeFolder = string.Join("_", safeFolder.Split(Path.GetInvalidFileNameChars()));

        // Partition by year/month for clean file organization
        var datePath = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        
        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        }

        var targetDirectory = Path.Combine(webRoot, "uploads", safeFolder, datePath);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // Sanitize original file name
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var rawName = Path.GetFileNameWithoutExtension(originalFileName);
        var cleanRawName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars()));
        if (cleanRawName.Length > 40) cleanRawName = cleanRawName.Substring(0, 40);

        var uniqueFileName = $"{Guid.NewGuid():N}_{cleanRawName}{ext}";
        var fullPhysicalPath = Path.Combine(targetDirectory, uniqueFileName);

        // Save stream to physical file
        using (var destStream = new FileStream(fullPhysicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(destStream, cancellationToken);
        }

        var relativeWebPath = $"/uploads/{safeFolder}/{datePath.Replace('\\', '/')}/{uniqueFileName}";

        _logger.LogInformation("Successfully stored engineering attachment: {OriginalName} -> {RelativePath} ({Size} bytes)",
            originalFileName, relativeWebPath, fileStream.Length);

        return new UploadedFileResult
        {
            FileName = originalFileName,
            FilePath = relativeWebPath,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            FileSizeBytes = fileStream.Length
        };
    }

    public Task<bool> DeleteFileAsync(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return Task.FromResult(false);

        try
        {
            var webRoot = _webHostEnvironment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            }

            var trimmed = relativeFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, trimmed);

            if (File.Exists(fullPath) && fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file: {Path}", fullPath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at: {RelativePath}", relativeFilePath);
        }

        return Task.FromResult(false);
    }
}
