using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Database;
using Android.OS;
using Android.Provider;
using DcimCloudSync.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;
using System.Security.Cryptography;
using AndroidOperationCanceledException = global::Android.OS.OperationCanceledException;
using DiagnosticDescriptorTraits = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.DiagnosticDescriptorTraits;
using DiagnosticEnvironmentSummary = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.DiagnosticEnvironmentSummary;
using DiagnosticMediaKind = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.DiagnosticMediaKind;
using DiagnosticMediaOpenPath = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.DiagnosticMediaOpenPath;
using DiagnosticResultCode = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.DiagnosticResultCode;
using FixtureDiagnosticObservation = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.FixtureDiagnosticObservation;
using MediaOpenPathObservation = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.MediaOpenPathObservation;
using PermissionScenarioExpectation = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.PermissionScenarioExpectation;
using OperationCanceledException = System.OperationCanceledException;
using VisualMediaPermissionScenario = DcimCloudSync.Diagnostics.Pixel7DiagnosticReport.VisualMediaPermissionScenario;

namespace DcimCloudSync.Platforms.Android.Diagnostics;

/// <summary>
/// Implements the temporary foreground-only, synthetic-only Pixel 7 MediaStore diagnostic.
/// </summary>
public sealed class AndroidPixel7MediaStoreDiagnostic : IPixel7MediaStoreDiagnostic
{
    private const int HashBufferSize = 64 * 1024;
    private const string DcimLikePattern = "DCIM/%";
    private const string ReadOnlyMode = "r";
    private const string TypedMimeFilter = "*/*";
    private const string UnknownEnvironmentValue = "unknown";

    private static readonly string[] Projection =
    [
        IBaseColumns.Id,
        MediaStore.IMediaColumns.VolumeName,
        MediaStore.IMediaColumns.DisplayName,
        MediaStore.IMediaColumns.RelativePath,
        MediaStore.IMediaColumns.MimeType,
        MediaStore.IMediaColumns.Size,
    ];

    /// <inheritdoc/>
    public Task<MediaAccessDiagnosticSnapshot> RefreshAccessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateAccessSnapshot());
    }

    /// <inheritdoc/>
    public async Task<MediaAccessDiagnosticSnapshot> RequestAccessAsync(
        VisualMediaAccessRequestOperation operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (operation == VisualMediaAccessRequestOperation.ManageSelection
            && !OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            return CreateAccessSnapshot();
        }

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return CreateAccessSnapshot();
        }

        switch (operation)
        {
            case VisualMediaAccessRequestOperation.Images:
                _ = await Permissions.RequestAsync<AndroidVisualMediaPermission.Images>();
                break;
            case VisualMediaAccessRequestOperation.Videos:
                _ = await Permissions.RequestAsync<AndroidVisualMediaPermission.Videos>();
                break;
            case VisualMediaAccessRequestOperation.Combined:
            case VisualMediaAccessRequestOperation.ManageSelection:
                _ = await Permissions.RequestAsync<AndroidVisualMediaPermission.Combined>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported access operation.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return CreateAccessSnapshot();
    }

    /// <inheritdoc/>
    public async Task<Pixel7DiagnosticReport> RunDiagnosticAsync(CancellationToken cancellationToken)
    {
        var runStopwatch = Stopwatch.StartNew();
        var access = CreateAccessSnapshot();

        DiagnosticFixtureManifest manifest;
        try
        {
            manifest = await DiagnosticFixtureManifest.LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return CreateEmptyReport(DiagnosticResultCode.Canceled, access, runStopwatch.Elapsed);
        }
        catch (InvalidDataException)
        {
            return CreateEmptyReport(DiagnosticResultCode.MalformedFixture, access, runStopwatch.Elapsed);
        }

        try
        {
            return await Task.Run(
                () => RunDiagnosticWorker(manifest, cancellationToken, runStopwatch));
        }
        catch (OperationCanceledException)
        {
            return CreateEmptyReport(
                DiagnosticResultCode.Canceled,
                CreateAccessSnapshot(),
                runStopwatch.Elapsed,
                manifest.FixtureSetVersion);
        }
        catch (Exception)
        {
            return CreateEmptyReport(
                DiagnosticResultCode.ProviderIoFailure,
                CreateAccessSnapshot(),
                runStopwatch.Elapsed,
                manifest.FixtureSetVersion);
        }
    }

    private static Pixel7DiagnosticReport RunDiagnosticWorker(
        DiagnosticFixtureManifest manifest,
        CancellationToken cancellationToken,
        Stopwatch runStopwatch)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var access = CreateAccessSnapshot();
        var scenario = GetScenario(access);
        var expectedFixtures = manifest.Fixtures
            .Where(fixture => IsExpectedForScenario(fixture, scenario, access))
            .ToArray();
        var scenarioExpectations = new[]
        {
            new PermissionScenarioExpectation(scenario, expectedFixtures.Select(fixture => fixture.Id)),
        };

        var context = global::Android.App.Application.Context;
        var resolver = context.ContentResolver;
        if (resolver is null)
        {
            return new Pixel7DiagnosticReport(
                DiagnosticResultCode.ProviderUnavailable,
                CreateEnvironment(context, manifest.FixtureSetVersion),
                access,
                scenarioExpectations,
                [],
                [],
                runStopwatch.Elapsed);
        }

        var activeReadHolder = new ActiveReadHolder();
        var imageState = new KindDiscoveryState(DiagnosticMediaKind.Image);
        var videoState = new KindDiscoveryState(DiagnosticMediaKind.Video);
        var collectionHandles = CreateCollectionHandles(context, imageState, videoState, cancellationToken);

        QueryCollections(
            resolver,
            collectionHandles,
            manifest,
            isDcimQuery: false,
            imageState,
            videoState,
            activeReadHolder,
            cancellationToken);
        QueryCollections(
            resolver,
            collectionHandles,
            manifest,
            isDcimQuery: true,
            imageState,
            videoState,
            activeReadHolder,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var discoveryObservations = new[]
        {
            CreateAggregateObservation(imageState, expectedFixtures, manifest),
            CreateAggregateObservation(videoState, expectedFixtures, manifest),
        };
        var fixtureObservations = new List<FixtureDiagnosticObservation>(manifest.Fixtures.Count);

        foreach (var fixture in manifest.Fixtures)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isExpected = expectedFixtures.Contains(fixture);
            var kindState = fixture.MediaKind == DiagnosticMediaKind.Image ? imageState : videoState;
            fixtureObservations.Add(CreateFixtureObservation(
                context,
                resolver,
                fixture,
                kindState,
                isExpected,
                scenario,
                activeReadHolder,
                cancellationToken));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var resultCode = DetermineOverallResult(
            access,
            discoveryObservations,
            fixtureObservations);

        return new Pixel7DiagnosticReport(
            resultCode,
            CreateEnvironment(context, manifest.FixtureSetVersion),
            access,
            scenarioExpectations,
            discoveryObservations,
            fixtureObservations,
            runStopwatch.Elapsed);
    }

    private static IReadOnlyList<CollectionHandle> CreateCollectionHandles(
        Context context,
        KindDiscoveryState imageState,
        KindDiscoveryState videoState,
        CancellationToken cancellationToken)
    {
        var handles = new List<CollectionHandle>();

        try
        {
            foreach (var volumeName in MediaStore.GetExternalVolumeNames(context))
            {
                cancellationToken.ThrowIfCancellationRequested();

                AddCollectionHandle(
                    volumeName,
                    DiagnosticMediaKind.Image,
                    MediaStore.Images.Media.GetContentUri(volumeName),
                    imageState,
                    handles);
                AddCollectionHandle(
                    volumeName,
                    DiagnosticMediaKind.Video,
                    MediaStore.Video.Media.GetContentUri(volumeName),
                    videoState,
                    handles);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Java.Lang.SecurityException)
        {
            imageState.ResultCode = DiagnosticResultCode.AccessDenied;
            videoState.ResultCode = DiagnosticResultCode.AccessDenied;
        }
        catch (Java.Lang.Exception)
        {
            imageState.ResultCode = DiagnosticResultCode.ProviderUnavailable;
            videoState.ResultCode = DiagnosticResultCode.ProviderUnavailable;
        }

        if (handles.Count == 0
            && imageState.ResultCode == DiagnosticResultCode.Success
            && videoState.ResultCode == DiagnosticResultCode.Success)
        {
            imageState.ResultCode = DiagnosticResultCode.CollectionUnavailable;
            videoState.ResultCode = DiagnosticResultCode.CollectionUnavailable;
        }

        return handles;
    }

    private static void AddCollectionHandle(
        string volumeName,
        DiagnosticMediaKind mediaKind,
        global::Android.Net.Uri? collectionUri,
        KindDiscoveryState state,
        List<CollectionHandle> handles)
    {
        if (collectionUri is null)
        {
            state.ResultCode = MergeResultCode(
                state.ResultCode,
                DiagnosticResultCode.CollectionUnavailable);
            return;
        }

        handles.Add(new CollectionHandle(volumeName, mediaKind, collectionUri));
    }

    private static void QueryCollections(
        ContentResolver resolver,
        IReadOnlyList<CollectionHandle> handles,
        DiagnosticFixtureManifest manifest,
        bool isDcimQuery,
        KindDiscoveryState imageState,
        KindDiscoveryState videoState,
        ActiveReadHolder activeReadHolder,
        CancellationToken cancellationToken)
    {
        foreach (var handle in handles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var state = handle.MediaKind == DiagnosticMediaKind.Image ? imageState : videoState;
            var names = manifest.Fixtures
                .Where(fixture => fixture.MediaKind == handle.MediaKind)
                .Select(fixture => fixture.DisplayName)
                .ToArray();
            var queryOutcome = QueryKnownFixtures(
                resolver,
                handle,
                names,
                isDcimQuery,
                activeReadHolder,
                cancellationToken);

            state.ResultCode = MergeResultCode(state.ResultCode, queryOutcome.ResultCode);
            state.Duration += queryOutcome.Duration;
            if (isDcimQuery)
            {
                state.DcimRows.AddRange(queryOutcome.Rows);
            }
            else
            {
                state.ControlRows.AddRange(queryOutcome.Rows);
            }
        }
    }

    private static QueryOutcome QueryKnownFixtures(
        ContentResolver resolver,
        CollectionHandle handle,
        IReadOnlyList<string> names,
        bool isDcimQuery,
        ActiveReadHolder activeReadHolder,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var rows = new List<MediaRow>();
        var placeholders = string.Join(", ", names.Select(static _ => "?"));
        var nameSelection = $"{MediaStore.IMediaColumns.DisplayName} IN ({placeholders})";
        var selection = isDcimQuery
            ? $"{MediaStore.IMediaColumns.RelativePath} LIKE ? AND {nameSelection}"
            : nameSelection;
        var selectionArguments = isDcimQuery
            ? new[] { DcimLikePattern }.Concat(names).ToArray()
            : names.ToArray();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var queryArguments = new Bundle();
            queryArguments.PutString(ContentResolver.QueryArgSqlSelection, selection);
            queryArguments.PutStringArray(ContentResolver.QueryArgSqlSelectionArgs, selectionArguments);
            queryArguments.PutString(
                ContentResolver.QueryArgSqlSortOrder,
                $"{IBaseColumns.Id} ASC");

            using var cancellationSignal = new CancellationSignal();
            var cancellationBridge = new CancellationBridge(cancellationSignal, activeReadHolder);
            using var cancellationRegistration = cancellationToken.Register(
                static state => ((CancellationBridge)state!).Cancel(),
                cancellationBridge);
            using var cursor = resolver.Query(
                handle.CollectionUri,
                Projection,
                queryArguments,
                cancellationSignal);

            if (cursor is null)
            {
                return new QueryOutcome(DiagnosticResultCode.ProviderUnavailable, rows, stopwatch.Elapsed);
            }

            var columns = GetProjectionColumns(cursor);
            if (columns is null)
            {
                return new QueryOutcome(DiagnosticResultCode.ProviderUnavailable, rows, stopwatch.Elapsed);
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!cursor.MoveToNext())
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var displayName = GetNullableString(cursor, columns.DisplayName);
                var volumeName = GetNullableString(cursor, columns.VolumeName);
                if (displayName is null
                    || volumeName is null
                    || !string.Equals(volumeName, handle.VolumeName, StringComparison.Ordinal))
                {
                    return new QueryOutcome(DiagnosticResultCode.ProviderUnavailable, rows, stopwatch.Elapsed);
                }

                rows.Add(new MediaRow(
                    handle.MediaKind,
                    cursor.GetLong(columns.Id),
                    volumeName,
                    displayName,
                    GetNullableString(cursor, columns.RelativePath),
                    GetNullableString(cursor, columns.MimeType),
                    cursor.IsNull(columns.Size) ? -1 : cursor.GetLong(columns.Size)));
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new QueryOutcome(DiagnosticResultCode.Success, rows, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return new QueryOutcome(DiagnosticResultCode.Canceled, rows, stopwatch.Elapsed);
        }
        catch (AndroidOperationCanceledException)
        {
            return new QueryOutcome(DiagnosticResultCode.Canceled, rows, stopwatch.Elapsed);
        }
        catch (Java.Lang.SecurityException)
        {
            return new QueryOutcome(DiagnosticResultCode.AccessDenied, rows, stopwatch.Elapsed);
        }
        catch (Java.Lang.UnsupportedOperationException)
        {
            return new QueryOutcome(DiagnosticResultCode.ProviderUnavailable, rows, stopwatch.Elapsed);
        }
        catch (Java.IO.FileNotFoundException)
        {
            return new QueryOutcome(DiagnosticResultCode.CollectionUnavailable, rows, stopwatch.Elapsed);
        }
        catch (Java.IO.IOException)
        {
            return new QueryOutcome(DiagnosticResultCode.ProviderIoFailure, rows, stopwatch.Elapsed);
        }
        catch (Java.Lang.Exception)
        {
            return new QueryOutcome(DiagnosticResultCode.ProviderUnavailable, rows, stopwatch.Elapsed);
        }
    }

    private static ProjectionColumns? GetProjectionColumns(ICursor cursor)
    {
        var id = cursor.GetColumnIndex(IBaseColumns.Id);
        var volumeName = cursor.GetColumnIndex(MediaStore.IMediaColumns.VolumeName);
        var displayName = cursor.GetColumnIndex(MediaStore.IMediaColumns.DisplayName);
        var relativePath = cursor.GetColumnIndex(MediaStore.IMediaColumns.RelativePath);
        var mimeType = cursor.GetColumnIndex(MediaStore.IMediaColumns.MimeType);
        var size = cursor.GetColumnIndex(MediaStore.IMediaColumns.Size);

        return id < 0 || volumeName < 0 || displayName < 0 || relativePath < 0 || mimeType < 0 || size < 0
            ? null
            : new ProjectionColumns(id, volumeName, displayName, relativePath, mimeType, size);
    }

    private static string? GetNullableString(ICursor cursor, int columnIndex)
    {
        return cursor.IsNull(columnIndex) ? null : cursor.GetString(columnIndex);
    }

    private static Pixel7DiagnosticReport.AggregateDiscoveryObservation CreateAggregateObservation(
        KindDiscoveryState state,
        IReadOnlyList<DiagnosticFixtureManifest.DiagnosticFixture> expectedFixtures,
        DiagnosticFixtureManifest manifest)
    {
        var expectedForKind = expectedFixtures
            .Where(fixture => fixture.MediaKind == state.MediaKind)
            .ToArray();
        var expectedVisibleIds = expectedForKind.Select(fixture => fixture.Id).ToHashSet(StringComparer.Ordinal);
        var expectedDcimIds = expectedForKind
            .Where(IsDcimFixture)
            .Select(fixture => fixture.Id)
            .ToHashSet(StringComparer.Ordinal);
        var fixturesByName = manifest.Fixtures
            .Where(fixture => fixture.MediaKind == state.MediaKind)
            .ToDictionary(fixture => fixture.DisplayName, StringComparer.Ordinal);
        var actualVisibleIds = GetFixtureIds(state.ControlRows, fixturesByName);
        var actualDcimIds = GetFixtureIds(state.DcimRows, fixturesByName);
        var unexpectedControlCount = state.DcimRows
            .Select(row => fixturesByName.GetValueOrDefault(row.DisplayName))
            .Where(fixture => fixture is not null && !IsDcimFixture(fixture))
            .Select(fixture => fixture!.Id)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var resultCode = state.ResultCode;
        if (resultCode == DiagnosticResultCode.Success && unexpectedControlCount > 0)
        {
            resultCode = DiagnosticResultCode.UnexpectedControlFixture;
        }
        else if (resultCode == DiagnosticResultCode.Success
            && (!expectedVisibleIds.SetEquals(actualVisibleIds)
                || !expectedDcimIds.SetEquals(actualDcimIds)))
        {
            resultCode = expectedVisibleIds.Except(actualVisibleIds).Any()
                || expectedDcimIds.Except(actualDcimIds).Any()
                ? DiagnosticResultCode.FixtureNotFound
                : DiagnosticResultCode.PartialAccess;
        }

        return new Pixel7DiagnosticReport.AggregateDiscoveryObservation(
            state.MediaKind,
            resultCode,
            expectedVisibleIds.Count,
            actualVisibleIds.Count,
            expectedDcimIds.Count,
            actualDcimIds.Count,
            unexpectedControlCount,
            state.Duration);
    }

    private static HashSet<string> GetFixtureIds(
        IEnumerable<MediaRow> rows,
        IReadOnlyDictionary<string, DiagnosticFixtureManifest.DiagnosticFixture> fixturesByName)
    {
        return rows
            .Select(row => fixturesByName.GetValueOrDefault(row.DisplayName)?.Id)
            .Where(static id => id is not null)
            .Select(static id => id!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static FixtureDiagnosticObservation CreateFixtureObservation(
        Context context,
        ContentResolver resolver,
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        KindDiscoveryState state,
        bool isExpected,
        VisualMediaPermissionScenario scenario,
        ActiveReadHolder activeReadHolder,
        CancellationToken cancellationToken)
    {
        if (!isExpected)
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                scenario == VisualMediaPermissionScenario.Denied
                    ? DiagnosticResultCode.AccessDenied
                    : DiagnosticResultCode.PartialAccess,
                []);
        }

        if (state.ResultCode != DiagnosticResultCode.Success)
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                state.ResultCode,
                []);
        }

        var matchingRows = state.ControlRows
            .Where(row => string.Equals(row.DisplayName, fixture.DisplayName, StringComparison.Ordinal))
            .ToArray();
        if (matchingRows.Length != 1)
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                matchingRows.Length == 0
                    ? DiagnosticResultCode.FixtureNotFound
                    : DiagnosticResultCode.ProviderUnavailable,
                []);
        }

        var row = matchingRows[0];
        var appearsInDcim = state.DcimRows.Any(candidate =>
            candidate.RowId == row.RowId
            && string.Equals(candidate.VolumeName, row.VolumeName, StringComparison.Ordinal));
        if (!string.Equals(row.RelativePath, fixture.ExpectedRelativePlacement, StringComparison.Ordinal)
            || appearsInDcim != IsDcimFixture(fixture))
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                appearsInDcim && !IsDcimFixture(fixture)
                    ? DiagnosticResultCode.UnexpectedControlFixture
                    : DiagnosticResultCode.FixtureNotFound,
                []);
        }

        if (string.IsNullOrWhiteSpace(row.MimeType)
            || !string.Equals(row.MimeType, fixture.ExpectedMimeType, StringComparison.Ordinal))
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                DiagnosticResultCode.InvalidMimeMetadata,
                CreateInvalidMimeObservations(fixture));
        }

        var currentAccess = CreateAccessSnapshot();
        if (!CanAccessFixture(currentAccess, fixture))
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                DiagnosticResultCode.AccessDenied,
                CreateOpenFailureObservations(fixture, DiagnosticResultCode.AccessDenied));
        }

        var itemUri = fixture.MediaKind == DiagnosticMediaKind.Image
            ? MediaStore.Images.Media.GetContentUri(row.VolumeName, row.RowId)
            : MediaStore.Video.Media.GetContentUri(row.VolumeName, row.RowId);
        var currentMimeResult = InspectCurrentMime(
            resolver,
            itemUri,
            fixture.ExpectedMimeType,
            cancellationToken);
        if (currentMimeResult != DiagnosticResultCode.Success)
        {
            return new FixtureDiagnosticObservation(
                fixture.Id,
                fixture.MediaKind,
                currentMimeResult,
                CreateOpenFailureObservations(fixture, currentMimeResult));
        }

        var openPathObservations = new[]
        {
            RunOrdinaryOpen(resolver, itemUri, fixture, activeReadHolder, cancellationToken),
            RunStrictTypedOpen(resolver, itemUri, fixture, activeReadHolder, cancellationToken),
            RunOriginalFormatFileDescriptorOpen(
                context,
                resolver,
                itemUri,
                fixture,
                activeReadHolder,
                cancellationToken),
        };

        return new FixtureDiagnosticObservation(
            fixture.Id,
            fixture.MediaKind,
            DiagnosticResultCode.Success,
            openPathObservations);
    }

    private static IReadOnlyList<MediaOpenPathObservation> CreateInvalidMimeObservations(
        DiagnosticFixtureManifest.DiagnosticFixture fixture)
    {
        return CreateOpenFailureObservations(fixture, DiagnosticResultCode.InvalidMimeMetadata);
    }

    private static IReadOnlyList<MediaOpenPathObservation> CreateOpenFailureObservations(
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        DiagnosticResultCode resultCode)
    {
        return Enum.GetValues<DiagnosticMediaOpenPath>()
            .Select(path => CreateOpenFailure(
                path,
                resultCode,
                fixture,
                TimeSpan.Zero))
            .ToArray();
    }

    private static MediaOpenPathObservation RunOrdinaryOpen(
        ContentResolver resolver,
        global::Android.Net.Uri itemUri,
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        ActiveReadHolder activeReadHolder,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var cancellationSignal = new CancellationSignal();
            var cancellationBridge = new CancellationBridge(cancellationSignal, activeReadHolder);
            using var cancellationRegistration = cancellationToken.Register(
                static state => ((CancellationBridge)state!).Cancel(),
                cancellationBridge);
            using var stream = resolver.OpenInputStream(itemUri);
            if (stream is null)
            {
                return CreateOpenFailure(
                    DiagnosticMediaOpenPath.Ordinary,
                    DiagnosticResultCode.ProviderUnavailable,
                    fixture,
                    stopwatch.Elapsed);
            }

            activeReadHolder.Set(stream);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var traits = GetStreamTraits(stream);
                var hash = HashSequentially(stream, cancellationToken);
                return CreateOpenResult(
                    DiagnosticMediaOpenPath.Ordinary,
                    fixture,
                    traits,
                    hash,
                    stopwatch.Elapsed);
            }
            finally
            {
                activeReadHolder.Clear(stream);
            }
        }
        catch (Exception exception)
        {
            return CreateOpenFailure(
                DiagnosticMediaOpenPath.Ordinary,
                MapOpenException(exception, cancellationToken),
                fixture,
                stopwatch.Elapsed);
        }
    }

    private static MediaOpenPathObservation RunStrictTypedOpen(
        ContentResolver resolver,
        global::Android.Net.Uri itemUri,
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        ActiveReadHolder activeReadHolder,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var originalUri = MediaStore.SetRequireOriginal(itemUri);

            using var options = new Bundle();
            options.PutBoolean(MediaStore.ExtraAcceptOriginalMediaFormat, true);
            using var cancellationSignal = new CancellationSignal();
            var cancellationBridge = new CancellationBridge(cancellationSignal, activeReadHolder);
            using var cancellationRegistration = cancellationToken.Register(
                static state => ((CancellationBridge)state!).Cancel(),
                cancellationBridge);
            using var descriptor = OperatingSystem.IsAndroidVersionAtLeast(36)
                ? MediaStore.OpenTypedAssetFileDescriptor(
                    resolver,
                    originalUri,
                    TypedMimeFilter,
                    options,
                    cancellationSignal)
                : resolver.OpenTypedAssetFileDescriptor(
                    originalUri,
                    TypedMimeFilter,
                    options,
                    cancellationSignal);
            if (descriptor is null)
            {
                return CreateOpenFailure(
                    DiagnosticMediaOpenPath.StrictTyped,
                    DiagnosticResultCode.ProviderUnavailable,
                    fixture,
                    stopwatch.Elapsed);
            }

            activeReadHolder.Set(descriptor);
            try
            {
                using var stream = descriptor.CreateInputStream();
                if (stream is null)
                {
                    return CreateOpenFailure(
                        DiagnosticMediaOpenPath.StrictTyped,
                        DiagnosticResultCode.ProviderUnavailable,
                        fixture,
                        stopwatch.Elapsed);
                }

                activeReadHolder.Set(stream);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var traits = new DiagnosticDescriptorTraits(
                        descriptor.DeclaredLength >= 0 ? descriptor.DeclaredLength : -1,
                        stream.CanSeek);
                    var hash = HashSequentially(stream, cancellationToken);
                    return CreateOpenResult(
                        DiagnosticMediaOpenPath.StrictTyped,
                        fixture,
                        traits,
                        hash,
                        stopwatch.Elapsed);
                }
                finally
                {
                    activeReadHolder.Clear(stream);
                }
            }
            finally
            {
                activeReadHolder.Clear(descriptor);
            }
        }
        catch (Exception exception)
        {
            return CreateOpenFailure(
                DiagnosticMediaOpenPath.StrictTyped,
                MapOpenException(exception, cancellationToken),
                fixture,
                stopwatch.Elapsed);
        }
    }

    private static MediaOpenPathObservation RunOriginalFormatFileDescriptorOpen(
        Context context,
        ContentResolver resolver,
        global::Android.Net.Uri itemUri,
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        ActiveReadHolder activeReadHolder,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var originalUri = MediaStore.SetRequireOriginal(itemUri);

            using var cancellationSignal = new CancellationSignal();
            var cancellationBridge = new CancellationBridge(cancellationSignal, activeReadHolder);
            using var cancellationRegistration = cancellationToken.Register(
                static state => ((CancellationBridge)state!).Cancel(),
                cancellationBridge);
            using var inputDescriptor = resolver.OpenFileDescriptor(
                originalUri,
                ReadOnlyMode,
                cancellationSignal);
            if (inputDescriptor is null)
            {
                return CreateOpenFailure(
                    DiagnosticMediaOpenPath.OriginalFormatFileDescriptor,
                    DiagnosticResultCode.ProviderUnavailable,
                    fixture,
                    stopwatch.Elapsed);
            }

            activeReadHolder.Set(inputDescriptor);
            ParcelFileDescriptor? originalDescriptor = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                originalDescriptor = MediaStore.GetOriginalMediaFormatFileDescriptor(context, inputDescriptor);
                if (originalDescriptor is null)
                {
                    activeReadHolder.Clear(inputDescriptor);
                    return CreateOpenFailure(
                        DiagnosticMediaOpenPath.OriginalFormatFileDescriptor,
                        DiagnosticResultCode.ProviderUnavailable,
                        fixture,
                        stopwatch.Elapsed);
                }

                activeReadHolder.Set(originalDescriptor);
                var reportedLength = originalDescriptor.StatSize >= 0
                    ? originalDescriptor.StatSize
                    : -1;
                Stream? stream = null;
                try
                {
                    stream = new global::Android.Runtime.InputStreamInvoker(
                        new ParcelFileDescriptor.AutoCloseInputStream(originalDescriptor));
                    originalDescriptor = null;
                    activeReadHolder.Set(stream);
                    cancellationToken.ThrowIfCancellationRequested();

                    var traits = new DiagnosticDescriptorTraits(reportedLength, stream.CanSeek);
                    var hash = HashSequentially(stream, cancellationToken);
                    return CreateOpenResult(
                        DiagnosticMediaOpenPath.OriginalFormatFileDescriptor,
                        fixture,
                        traits,
                        hash,
                        stopwatch.Elapsed);
                }
                finally
                {
                    if (stream is not null)
                    {
                        activeReadHolder.Clear(stream);
                        stream.Dispose();
                    }

                    originalDescriptor?.Dispose();
                }
            }
            finally
            {
                activeReadHolder.Clear(inputDescriptor);
            }
        }
        catch (Exception exception)
        {
            return CreateOpenFailure(
                DiagnosticMediaOpenPath.OriginalFormatFileDescriptor,
                MapOpenException(exception, cancellationToken),
                fixture,
                stopwatch.Elapsed);
        }
    }

    private static HashObservation HashSequentially(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[HashBufferSize];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long byteCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            cancellationToken.ThrowIfCancellationRequested();

            if (bytesRead == 0)
            {
                break;
            }

            hash.AppendData(buffer, 0, bytesRead);
            byteCount += bytesRead;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new HashObservation(
            byteCount,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private static DiagnosticResultCode InspectCurrentMime(
        ContentResolver resolver,
        global::Android.Net.Uri itemUri,
        string expectedMimeType,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentMimeType = resolver.GetType(itemUri);
            cancellationToken.ThrowIfCancellationRequested();

            return string.IsNullOrWhiteSpace(currentMimeType)
                || !string.Equals(currentMimeType, expectedMimeType, StringComparison.Ordinal)
                ? DiagnosticResultCode.InvalidMimeMetadata
                : DiagnosticResultCode.Success;
        }
        catch (Exception exception)
        {
            return MapOpenException(exception, cancellationToken);
        }
    }

    private static DiagnosticDescriptorTraits GetStreamTraits(Stream stream)
    {
        var canSeek = stream.CanSeek;
        if (!canSeek)
        {
            return new DiagnosticDescriptorTraits(-1, false);
        }

        try
        {
            return new DiagnosticDescriptorTraits(stream.Length, true);
        }
        catch (NotSupportedException)
        {
            return new DiagnosticDescriptorTraits(-1, true);
        }
        catch (System.IO.IOException)
        {
            return new DiagnosticDescriptorTraits(-1, true);
        }
    }

    private static MediaOpenPathObservation CreateOpenResult(
        DiagnosticMediaOpenPath path,
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        DiagnosticDescriptorTraits traits,
        HashObservation hash,
        TimeSpan duration)
    {
        var resultCode = hash.ByteCount != fixture.ExpectedByteCount
            ? DiagnosticResultCode.ByteCountMismatch
            : !string.Equals(hash.Sha256, fixture.ExpectedSha256, StringComparison.Ordinal)
                ? DiagnosticResultCode.HashMismatch
                : DiagnosticResultCode.Success;

        return new MediaOpenPathObservation(
            path,
            resultCode,
            fixture.ExpectedByteCount,
            hash.ByteCount,
            fixture.ExpectedSha256,
            hash.Sha256,
            traits,
            duration);
    }

    private static MediaOpenPathObservation CreateOpenFailure(
        DiagnosticMediaOpenPath path,
        DiagnosticResultCode resultCode,
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        TimeSpan duration)
    {
        return new MediaOpenPathObservation(
            path,
            resultCode,
            fixture.ExpectedByteCount,
            null,
            fixture.ExpectedSha256,
            null,
            new DiagnosticDescriptorTraits(-1, false),
            duration);
    }

    private static DiagnosticResultCode MapOpenException(
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested
            || exception is OperationCanceledException
            || exception is AndroidOperationCanceledException
            || exception is ObjectDisposedException && cancellationToken.IsCancellationRequested)
        {
            return DiagnosticResultCode.Canceled;
        }

        return exception switch
        {
            Java.Lang.SecurityException => DiagnosticResultCode.AccessDenied,
            UnauthorizedAccessException => DiagnosticResultCode.AccessDenied,
            Java.Lang.UnsupportedOperationException => DiagnosticResultCode.OriginalFormatUnsupported,
            Java.IO.FileNotFoundException => DiagnosticResultCode.FixtureNotFound,
            Java.IO.IOException => DiagnosticResultCode.ProviderIoFailure,
            System.IO.IOException => DiagnosticResultCode.ProviderIoFailure,
            Java.Lang.IllegalArgumentException => DiagnosticResultCode.ProviderUnavailable,
            _ => DiagnosticResultCode.ProviderUnavailable,
        };
    }

    private static DiagnosticResultCode DetermineOverallResult(
        MediaAccessDiagnosticSnapshot access,
        IEnumerable<Pixel7DiagnosticReport.AggregateDiscoveryObservation> discoveryObservations,
        IEnumerable<FixtureDiagnosticObservation> fixtureObservations)
    {
        var discoveryFailure = discoveryObservations
            .Select(observation => observation.ResultCode)
            .FirstOrDefault(resultCode => resultCode != DiagnosticResultCode.Success);
        if (discoveryFailure != DiagnosticResultCode.Success)
        {
            return discoveryFailure;
        }

        var openFailure = fixtureObservations
            .SelectMany(observation => observation.OpenPathObservations)
            .Select(observation => observation.ResultCode)
            .FirstOrDefault(resultCode => resultCode != DiagnosticResultCode.Success);
        if (openFailure != DiagnosticResultCode.Success)
        {
            return openFailure;
        }

        var fixtureFailure = fixtureObservations
            .Where(observation => observation.DiscoveryResultCode != DiagnosticResultCode.PartialAccess
                && observation.DiscoveryResultCode != DiagnosticResultCode.AccessDenied)
            .Select(observation => observation.DiscoveryResultCode)
            .FirstOrDefault(resultCode => resultCode != DiagnosticResultCode.Success);
        if (fixtureFailure != DiagnosticResultCode.Success)
        {
            return fixtureFailure;
        }

        return access.Completeness switch
        {
            VisualMediaAccessCompleteness.Complete => DiagnosticResultCode.Success,
            VisualMediaAccessCompleteness.Partial => DiagnosticResultCode.PartialAccess,
            _ => DiagnosticResultCode.AccessDenied,
        };
    }

    private static MediaAccessDiagnosticSnapshot CreateAccessSnapshot()
    {
        var runtimeApiLevel = (int)Build.VERSION.SdkInt;
        var context = global::Android.App.Application.Context;
        var imageGranted = IsPermissionGranted(context, global::Android.Manifest.Permission.ReadMediaImages);
        var videoGranted = IsPermissionGranted(context, global::Android.Manifest.Permission.ReadMediaVideo);
        var supportsSelectedAccess = OperatingSystem.IsAndroidVersionAtLeast(34);
        var selectedGranted = supportsSelectedAccess
            && IsPermissionGranted(
                context,
                global::Android.Manifest.Permission.ReadMediaVisualUserSelected);

        return new MediaAccessDiagnosticSnapshot(
            runtimeApiLevel,
            imageGranted ? VisualMediaGrantStatus.Granted : VisualMediaGrantStatus.Denied,
            videoGranted ? VisualMediaGrantStatus.Granted : VisualMediaGrantStatus.Denied,
            !supportsSelectedAccess
                ? SelectedVisualMediaAccessStatus.NotSupported
                : selectedGranted
                    ? SelectedVisualMediaAccessStatus.Granted
                    : SelectedVisualMediaAccessStatus.Denied,
            Permissions.ShouldShowRationale<AndroidVisualMediaPermission.Images>(),
            Permissions.ShouldShowRationale<AndroidVisualMediaPermission.Videos>(),
            supportsSelectedAccess
                && Permissions.ShouldShowRationale<AndroidVisualMediaPermission.Selected>(),
            supportsSelectedAccess);
    }

    private static bool IsPermissionGranted(Context context, string permission)
    {
        return context.CheckSelfPermission(permission) == Permission.Granted;
    }

    private static VisualMediaPermissionScenario GetScenario(MediaAccessDiagnosticSnapshot access)
    {
        var imagesGranted = access.ImageAccess == VisualMediaGrantStatus.Granted;
        var videosGranted = access.VideoAccess == VisualMediaGrantStatus.Granted;
        var selectedGranted = access.SelectedAccess == SelectedVisualMediaAccessStatus.Granted;

        if (imagesGranted && videosGranted)
        {
            return VisualMediaPermissionScenario.Full;
        }

        if (selectedGranted && (imagesGranted || videosGranted))
        {
            return VisualMediaPermissionScenario.Mixed;
        }

        if (imagesGranted)
        {
            return VisualMediaPermissionScenario.ImagesOnly;
        }

        if (videosGranted)
        {
            return VisualMediaPermissionScenario.VideosOnly;
        }

        return selectedGranted
            ? VisualMediaPermissionScenario.SelectedOnly
            : VisualMediaPermissionScenario.Denied;
    }

    private static bool IsExpectedForScenario(
        DiagnosticFixtureManifest.DiagnosticFixture fixture,
        VisualMediaPermissionScenario scenario,
        MediaAccessDiagnosticSnapshot access)
    {
        return scenario switch
        {
            VisualMediaPermissionScenario.Full => fixture.ExpectedVisibility.Full,
            VisualMediaPermissionScenario.ImagesOnly => fixture.ExpectedVisibility.ImagesOnly,
            VisualMediaPermissionScenario.VideosOnly => fixture.ExpectedVisibility.VideosOnly,
            VisualMediaPermissionScenario.SelectedOnly => fixture.ExpectedVisibility.SelectedOnly,
            VisualMediaPermissionScenario.Denied => fixture.ExpectedVisibility.Denied,
            VisualMediaPermissionScenario.Mixed =>
                fixture.SelectedForLimitedAccess
                || fixture.MediaKind == DiagnosticMediaKind.Image
                    && access.ImageAccess == VisualMediaGrantStatus.Granted
                || fixture.MediaKind == DiagnosticMediaKind.Video
                    && access.VideoAccess == VisualMediaGrantStatus.Granted,
            _ => false,
        };
    }

    private static bool CanAccessFixture(
        MediaAccessDiagnosticSnapshot access,
        DiagnosticFixtureManifest.DiagnosticFixture fixture)
    {
        var broadGrant = fixture.MediaKind == DiagnosticMediaKind.Image
            ? access.ImageAccess == VisualMediaGrantStatus.Granted
            : access.VideoAccess == VisualMediaGrantStatus.Granted;
        var selectedGrant = access.SelectedAccess == SelectedVisualMediaAccessStatus.Granted
            && fixture.SelectedForLimitedAccess;

        return broadGrant || selectedGrant;
    }

    private static bool IsDcimFixture(DiagnosticFixtureManifest.DiagnosticFixture fixture)
    {
        return fixture.ExpectedRelativePlacement.StartsWith("DCIM/", StringComparison.Ordinal);
    }

    private static DiagnosticResultCode MergeResultCode(
        DiagnosticResultCode current,
        DiagnosticResultCode candidate)
    {
        if (current == DiagnosticResultCode.Canceled || candidate == DiagnosticResultCode.Success)
        {
            return current;
        }

        return candidate == DiagnosticResultCode.Canceled || current == DiagnosticResultCode.Success
            ? candidate
            : current;
    }

    private static Pixel7DiagnosticReport CreateEmptyReport(
        DiagnosticResultCode resultCode,
        MediaAccessDiagnosticSnapshot access,
        TimeSpan duration,
        string fixtureSetVersion = UnknownEnvironmentValue)
    {
        var context = global::Android.App.Application.Context;
        return new Pixel7DiagnosticReport(
            resultCode,
            CreateEnvironment(context, fixtureSetVersion),
            access,
            [],
            [],
            [],
            duration);
    }

    private static DiagnosticEnvironmentSummary CreateEnvironment(
        Context context,
        string fixtureSetVersion)
    {
        var securityPatch = Build.VERSION.SecurityPatch;
        var securityPatchMonth = !string.IsNullOrWhiteSpace(securityPatch) && securityPatch.Length >= 7
            ? securityPatch[..7]
            : UnknownEnvironmentValue;
        var applicationInfo = context.ApplicationInfo;

        return new DiagnosticEnvironmentSummary(
            (int)Build.VERSION.SdkInt,
            applicationInfo is null ? 0 : (int)applicationInfo.TargetSdkVersion,
            Build.VERSION.Release ?? UnknownEnvironmentValue,
            securityPatchMonth,
            null,
            fixtureSetVersion);
    }

    private sealed class ActiveReadHolder
    {
        private readonly object syncRoot = new();
        private IDisposable? activeResource;
        private bool isCanceled;

        internal void Set(IDisposable resource)
        {
            ArgumentNullException.ThrowIfNull(resource);

            var disposeImmediately = false;
            lock (syncRoot)
            {
                if (isCanceled)
                {
                    disposeImmediately = true;
                }
                else
                {
                    activeResource = resource;
                }
            }

            if (disposeImmediately)
            {
                DisposeWithoutSurfacing(resource);
            }
        }

        internal void Clear(IDisposable resource)
        {
            lock (syncRoot)
            {
                if (ReferenceEquals(activeResource, resource))
                {
                    activeResource = null;
                }
            }
        }

        internal void Cancel()
        {
            IDisposable? resource;
            lock (syncRoot)
            {
                isCanceled = true;
                resource = activeResource;
                activeResource = null;
            }

            if (resource is not null)
            {
                DisposeWithoutSurfacing(resource);
            }
        }

        private static void DisposeWithoutSurfacing(IDisposable resource)
        {
            try
            {
                resource.Dispose();
            }
            catch (Exception)
            {
                // Cancellation cleanup must not surface provider-specific exception details.
            }
        }
    }

    private sealed record CancellationBridge(
        CancellationSignal CancellationSignal,
        ActiveReadHolder ActiveReadHolder)
    {
        internal void Cancel()
        {
            try
            {
                CancellationSignal.Cancel();
            }
            catch (Exception)
            {
                // Continue with active-resource closure if the provider signal fails.
            }

            ActiveReadHolder.Cancel();
        }
    }

    private sealed class KindDiscoveryState(DiagnosticMediaKind mediaKind)
    {
        internal DiagnosticMediaKind MediaKind { get; } = mediaKind;

        internal DiagnosticResultCode ResultCode { get; set; } = DiagnosticResultCode.Success;

        internal List<MediaRow> ControlRows { get; } = [];

        internal List<MediaRow> DcimRows { get; } = [];

        internal TimeSpan Duration { get; set; }
    }

    private sealed record CollectionHandle(
        string VolumeName,
        DiagnosticMediaKind MediaKind,
        global::Android.Net.Uri CollectionUri);

    private sealed record MediaRow(
        DiagnosticMediaKind MediaKind,
        long RowId,
        string VolumeName,
        string DisplayName,
        string? RelativePath,
        string? MimeType,
        long IndexedSize);

    private sealed record QueryOutcome(
        DiagnosticResultCode ResultCode,
        IReadOnlyList<MediaRow> Rows,
        TimeSpan Duration);

    private sealed record ProjectionColumns(
        int Id,
        int VolumeName,
        int DisplayName,
        int RelativePath,
        int MimeType,
        int Size);

    private sealed record HashObservation(long ByteCount, string Sha256);
}
