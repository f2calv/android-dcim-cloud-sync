using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DcimCloudSync.Services;

/// <summary>
/// Discovers DCIM photos and videos through Android MediaStore and opens their original bytes.
/// </summary>
/// <remarks>
/// Queries the image and video collections of every external volume for rows directly in DCIM, or, when
/// <see cref="MediaStoreMediaSourceConfig.SubfoldersIncluded"/> is set, in DCIM and its subfolders;
/// pending and trashed rows are excluded by MediaStore. When the app holds <c>ACCESS_MEDIA_LOCATION</c>, content opens
/// through <see cref="MediaStore.SetRequireOriginal(global::Android.Net.Uri)"/> and accepts the original format, so photo
/// location is kept and HEVC video is not transcoded; otherwise MediaStore returns location-redacted bytes. Only the rows
/// the user granted access to are visible. Content URIs and display names are never logged. Change tracking uses each
/// volume's MediaStore generation: a token records the version and generation of every external volume, and a later query
/// lists rows whose <c>generation_modified</c> is newer. A volume whose version changed, because its index was rebuilt, or
/// that the token does not name is listed in full.
/// </remarks>
/// <param name="config">Selects whether DCIM subfolders are read.</param>
public sealed class MediaStoreMediaSource(IOptionsMonitor<MediaStoreMediaSourceConfig> config) : IChangeTrackingMediaSource
{
    private const string AnyMimeType = "*/*";
    private const char TokenVolumeSeparator = '\n';
    private const char TokenFieldSeparator = '|';

    private static readonly string[] Projection =
    [
        IBaseColumns.Id,
        MediaStore.IMediaColumns.RelativePath,
        MediaStore.IMediaColumns.DisplayName,
        MediaStore.IMediaColumns.MimeType,
        MediaStore.IMediaColumns.Size,
        MediaStore.IMediaColumns.DateModified,
    ];

    /// <inheritdoc/>
    public IAsyncEnumerable<MediaItem> DiscoverAsync(CancellationToken cancellationToken) =>
        DiscoverSinceAsync(new Dictionary<string, long>(StringComparer.Ordinal), cancellationToken);

    /// <inheritdoc/>
    public string GetChangeToken()
    {
        var context = Platform.AppContext;
        return string.Join(
            TokenVolumeSeparator,
            MediaStore.GetExternalVolumeNames(context).Select(volumeName => string.Join(
                TokenFieldSeparator,
                volumeName,
                MediaStore.GetVersion(context, volumeName),
                MediaStore.GetGeneration(context, volumeName).ToString(CultureInfo.InvariantCulture))));
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<MediaItem> DiscoverChangedAsync(string sinceToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sinceToken);
        var context = Platform.AppContext;
        var generations = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var volume in sinceToken.Split(TokenVolumeSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = volume.Split(TokenFieldSeparator);
            if (fields.Length == 3
                && long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var generation)
                && MediaStore.GetExternalVolumeNames(context).Contains(fields[0])
                && string.Equals(MediaStore.GetVersion(context, fields[0]), fields[1], StringComparison.Ordinal))
                generations[fields[0]] = generation;
        }

        return DiscoverSinceAsync(generations, cancellationToken);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">The item did not come from this source.</exception>
    public Task<Stream> OpenReadAsync(MediaItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!MediaStoreItemPath.TryParseSourceId(item.SourceId, out var volumeName, out var collection, out var rowId))
            throw new ArgumentException("The media item did not come from MediaStore.", nameof(item));

        cancellationToken.ThrowIfCancellationRequested();
        var context = Platform.AppContext;
        var uri = GetCollectionUri(volumeName, collection, rowId);
        if (context.CheckSelfPermission(global::Android.Manifest.Permission.AccessMediaLocation) == Permission.Granted)
            uri = MediaStore.SetRequireOriginal(uri);

        using var options = new Bundle();
        options.PutBoolean(MediaStore.ExtraAcceptOriginalMediaFormat, true);
        using var cancellationSignal = new CancellationSignal();
        using var registration = cancellationToken.Register(static signal => ((CancellationSignal)signal!).Cancel(), cancellationSignal);
        try
        {
            var descriptor = OperatingSystem.IsAndroidVersionAtLeast(36)
                ? MediaStore.OpenTypedAssetFileDescriptor(context.ContentResolver!, uri, AnyMimeType, options, cancellationSignal)
                : context.ContentResolver!.OpenTypedAssetFileDescriptor(uri, AnyMimeType, options, cancellationSignal);

            // The returned stream closes the descriptor when disposed.
            var stream = descriptor?.CreateInputStream()
                ?? throw new IOException("MediaStore could not open the media item.");
            return Task.FromResult(stream);
        }
        catch (Java.IO.FileNotFoundException ex)
        {
            throw new IOException("The media item was removed or cannot be read.", ex);
        }
        catch (Java.Lang.SecurityException ex)
        {
            throw new UnauthorizedAccessException("Access to the media item was denied.", ex);
        }
        catch (global::Android.OS.OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new System.OperationCanceledException("Opening the media item was canceled.", ex, cancellationToken);
        }
    }

    private async IAsyncEnumerable<MediaItem> DiscoverSinceAsync(
        Dictionary<string, long> generations,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var context = Platform.AppContext;
        // LIKE without a wildcard matches DCIM itself case-insensitively; with one it also matches every subfolder.
        var pathPattern = config.CurrentValue.SubfoldersIncluded ? $"{MediaStoreItemPath.DcimPrefix}%" : MediaStoreItemPath.DcimPrefix;
        foreach (var volumeName in MediaStore.GetExternalVolumeNames(context))
        {
            long? sinceGeneration = generations.TryGetValue(volumeName, out var generation) ? generation : null;
            foreach (var collection in new[] { MediaStoreItemPath.ImagesCollection, MediaStoreItemPath.VideoCollection })
            {
                foreach (var item in Query(context.ContentResolver!, volumeName, collection, pathPattern, sinceGeneration, cancellationToken))
                    yield return item;

                await Task.Yield();
            }
        }
    }

    private static IEnumerable<MediaItem> Query(
        ContentResolver resolver,
        string volumeName,
        string collection,
        string pathPattern,
        long? sinceGeneration,
        CancellationToken cancellationToken)
    {
        using var queryArguments = new Bundle();
        if (sinceGeneration is { } generation)
        {
            queryArguments.PutString(
                ContentResolver.QueryArgSqlSelection,
                $"{MediaStore.IMediaColumns.RelativePath} LIKE ? AND {MediaStore.IMediaColumns.GenerationModified} > ?");
            queryArguments.PutStringArray(
                ContentResolver.QueryArgSqlSelectionArgs,
                [pathPattern, generation.ToString(CultureInfo.InvariantCulture)]);
        }
        else
        {
            queryArguments.PutString(ContentResolver.QueryArgSqlSelection, $"{MediaStore.IMediaColumns.RelativePath} LIKE ?");
            queryArguments.PutStringArray(ContentResolver.QueryArgSqlSelectionArgs, [pathPattern]);
        }

        queryArguments.PutString(ContentResolver.QueryArgSqlSortOrder, $"{IBaseColumns.Id} ASC");
        using var cancellationSignal = new CancellationSignal();
        using var registration = cancellationToken.Register(static signal => ((CancellationSignal)signal!).Cancel(), cancellationSignal);
        using var cursor = QueryCursor(resolver, GetCollectionUri(volumeName, collection), queryArguments, cancellationSignal, cancellationToken);
        if (cursor is null)
            yield break;

        var id = cursor.GetColumnIndexOrThrow(IBaseColumns.Id);
        var relativePath = cursor.GetColumnIndexOrThrow(MediaStore.IMediaColumns.RelativePath);
        var displayName = cursor.GetColumnIndexOrThrow(MediaStore.IMediaColumns.DisplayName);
        var mimeType = cursor.GetColumnIndexOrThrow(MediaStore.IMediaColumns.MimeType);
        var size = cursor.GetColumnIndexOrThrow(MediaStore.IMediaColumns.Size);
        var dateModified = cursor.GetColumnIndexOrThrow(MediaStore.IMediaColumns.DateModified);
        while (cursor.MoveToNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = cursor.GetString(displayName);
            var path = MediaStoreItemPath.GetRelativePath(volumeName, cursor.GetString(relativePath), name);
            var mime = cursor.GetString(mimeType);
            if (path is null || name is null || string.IsNullOrEmpty(mime))
                continue;

            yield return new MediaItem
            {
                SourceId = MediaStoreItemPath.CreateSourceId(volumeName, collection, cursor.GetLong(id)),
                RelativePath = path,
                DisplayName = name,
                MimeType = mime,
                SizeBytes = cursor.IsNull(size) ? 0 : cursor.GetLong(size),
                ModifiedUtc = DateTimeOffset.FromUnixTimeSeconds(cursor.IsNull(dateModified) ? 0 : cursor.GetLong(dateModified)),
            };
        }
    }

    private static global::Android.Database.ICursor? QueryCursor(
        ContentResolver resolver,
        global::Android.Net.Uri collectionUri,
        Bundle queryArguments,
        CancellationSignal cancellationSignal,
        CancellationToken cancellationToken)
    {
        try
        {
            return resolver.Query(collectionUri, Projection, queryArguments, cancellationSignal);
        }
        catch (global::Android.OS.OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new System.OperationCanceledException("Discovering media was canceled.", ex, cancellationToken);
        }
        catch (Java.Lang.SecurityException ex)
        {
            throw new UnauthorizedAccessException("Access to the media library was denied.", ex);
        }
    }

    private static global::Android.Net.Uri GetCollectionUri(string volumeName, string collection) =>
        (collection == MediaStoreItemPath.VideoCollection
            ? MediaStore.Video.Media.GetContentUri(volumeName)
            : MediaStore.Images.Media.GetContentUri(volumeName))
        ?? throw new IOException("MediaStore has no collection for the volume.");

    private static global::Android.Net.Uri GetCollectionUri(string volumeName, string collection, long rowId) =>
        (collection == MediaStoreItemPath.VideoCollection
            ? MediaStore.Video.Media.GetContentUri(volumeName, rowId)
            : MediaStore.Images.Media.GetContentUri(volumeName, rowId))
        ?? throw new IOException("MediaStore has no content URI for the item.");
}
