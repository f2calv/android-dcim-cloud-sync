using Microsoft.Extensions.Options;
using System.Collections.Frozen;

namespace DcimCloudSync.Services;

/// <summary>
/// Discovers photos and videos below a configured folder, standing in for Android MediaStore during development.
/// </summary>
/// <remarks>
/// Only files with a known photo or video extension are returned; hidden and system files and inaccessible folders
/// are skipped. <see cref="MediaItem.SourceId"/> is the item's relative path.
/// </remarks>
public sealed class FileSystemMediaSource(IOptions<FileSystemMediaSourceConfig> config) : IMediaSource
{
    private static readonly FrozenDictionary<string, string> MimeTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".3gp"] = "video/3gpp",
            [".dng"] = "image/x-adobe-dng",
            [".gif"] = "image/gif",
            [".heic"] = "image/heic",
            [".heif"] = "image/heif",
            [".jpeg"] = "image/jpeg",
            [".jpg"] = "image/jpeg",
            [".mkv"] = "video/x-matroska",
            [".mov"] = "video/quicktime",
            [".mp4"] = "video/mp4",
            [".png"] = "image/png",
            [".webm"] = "video/webm",
            [".webp"] = "image/webp",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly EnumerationOptions RecursiveEnumeration = new()
    {
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        IgnoreInaccessible = true,
        RecurseSubdirectories = true,
    };

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <inheritdoc/>
    /// <exception cref="DirectoryNotFoundException">The configured media folder does not exist.</exception>
    public IAsyncEnumerable<MediaItem> DiscoverAsync(CancellationToken cancellationToken) =>
        Discover(cancellationToken).ToAsyncEnumerable();

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(MediaItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(config.Value.RootPath));
        var fullPath = Path.GetFullPath(Path.Combine(root, item.RelativePath));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, PathComparison))
            throw new ArgumentException("The media item is outside the configured media folder.", nameof(item));

        Stream stream = new FileStream(fullPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            Share = FileShare.Read,
        });
        return Task.FromResult(stream);
    }

    private IEnumerable<MediaItem> Discover(CancellationToken cancellationToken)
    {
        var root = new DirectoryInfo(Path.GetFullPath(config.Value.RootPath));
        if (!root.Exists)
            throw new DirectoryNotFoundException("The configured media folder does not exist.");

        foreach (var file in root.EnumerateFiles("*", RecursiveEnumeration))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!MimeTypesByExtension.TryGetValue(file.Extension, out var mimeType))
                continue;

            var relativePath = Path.GetRelativePath(root.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/');
            yield return new MediaItem
            {
                SourceId = relativePath,
                RelativePath = relativePath,
                DisplayName = file.Name,
                MimeType = mimeType,
                SizeBytes = file.Length,
                ModifiedUtc = new DateTimeOffset(file.LastWriteTimeUtc),
            };
        }
    }
}
