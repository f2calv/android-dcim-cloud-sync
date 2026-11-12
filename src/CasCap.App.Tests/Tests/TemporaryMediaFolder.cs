namespace DcimCloudSync.Tests;

/// <summary>
/// Creates a disposable folder of synthetic media files and a <see cref="FileSystemMediaSource"/> rooted in it.
/// </summary>
internal sealed class TemporaryMediaFolder : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemporaryMediaFolder"/> class with an empty folder.
    /// </summary>
    public TemporaryMediaFolder()
    {
        RootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(RootPath);
        Source = CreateSource(RootPath);
    }

    /// <summary>Gets the folder's full path.</summary>
    public string RootPath { get; }

    /// <summary>Gets a media source rooted in the folder.</summary>
    public FileSystemMediaSource Source { get; }

    /// <summary>
    /// Creates a media source rooted in an arbitrary folder.
    /// </summary>
    /// <param name="rootPath">The folder to scan.</param>
    /// <returns>The media source.</returns>
    public static FileSystemMediaSource CreateSource(string rootPath) =>
        new(Options.Create(new FileSystemMediaSourceConfig { RootPath = rootPath }));

    /// <summary>
    /// Writes a synthetic file below the folder.
    /// </summary>
    /// <param name="relativePath">The file's path relative to the folder, using <c>/</c> separators.</param>
    /// <param name="content">The file's content.</param>
    /// <returns>The file's full path.</returns>
    public string AddFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    /// <inheritdoc/>
    public void Dispose() => Directory.Delete(RootPath, recursive: true);
}
