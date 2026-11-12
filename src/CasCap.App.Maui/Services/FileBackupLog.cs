using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Storage;

namespace DcimCloudSync.Services;

/// <summary>
/// Keeps the most recent backup and scan events in a JSON file in app-private storage.
/// </summary>
/// <remarks>
/// Entries name files, so the file stays in the app data directory, which the manifest excludes from Android backup, and is
/// never sent anywhere. A damaged file is replaced rather than failing a backup.
/// </remarks>
public sealed class FileBackupLog : IBackupLog
{
    /// <summary>The number of entries kept.</summary>
    public const int Capacity = 25;

    private readonly Lock _lock = new();
    private readonly string _path;
    private List<BackupLogEntry>? _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBackupLog"/> class in the app data directory.
    /// </summary>
    public FileBackupLog()
        : this(Path.Combine(FileSystem.Current.AppDataDirectory, "backup-log.json"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBackupLog"/> class at a given path.
    /// </summary>
    /// <param name="path">The log file path.</param>
    public FileBackupLog(string path) => _path = path;

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public void Add(BackupLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_lock)
        {
            var entries = Load();
            entries.Insert(0, entry);
            if (entries.Count > Capacity)
                entries.RemoveRange(Capacity, entries.Count - Capacity);
            Save(entries);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public IReadOnlyList<BackupLogEntry> GetEntries()
    {
        lock (_lock)
            return [.. Load()];
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            _entries = [];
            File.Delete(_path);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private List<BackupLogEntry> Load()
    {
        if (_entries is not null)
            return _entries;

        try
        {
            using var stream = File.OpenRead(_path);
            _entries = JsonSerializer.Deserialize(stream, BackupLogJsonContext.Default.ListBackupLogEntry) ?? [];
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or JsonException)
        {
            _entries = [];
        }

        return _entries;
    }

    private void Save(List<BackupLogEntry> entries)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temporary = _path + ".tmp";
            using (var stream = File.Create(temporary))
                JsonSerializer.Serialize(stream, entries, BackupLogJsonContext.Default.ListBackupLogEntry);
            File.Move(temporary, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The log is a convenience; losing an entry must never stop a backup.
        }
    }
}

/// <summary>
/// Generates trimming-safe JSON serialization for the backup log.
/// </summary>
[JsonSerializable(typeof(List<BackupLogEntry>))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
internal sealed partial class BackupLogJsonContext : JsonSerializerContext;
