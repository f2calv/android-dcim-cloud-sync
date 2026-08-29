using Microsoft.Maui.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DcimCloudSync.Diagnostics;

/// <summary>
/// Loads and validates the internal packaged synthetic-fixture contract for the temporary diagnostic.
/// </summary>
/// <remarks>
/// The manifest is consumed only by explicit foreground diagnostic activity. Display names and relative
/// placements remain internal and must never cross into the sanitized report contract or application logs.
/// </remarks>
internal sealed record DiagnosticFixtureManifest
{
    private const string AssetName = "diagnostic-fixtures.json";
    private const string SupportedFixtureSetVersion = "pixel7-mediastore-v1";
    private const int SupportedManifestVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly ExpectedFixtureDefinition[] AllowedFixtures =
    [
        new(
            "jpeg-dcim-nested-v1",
            "dcs-diag-v1-jpeg-dcim-nested.jpg",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Image,
            "DCIM/DcimCloudSyncDiagnostic/Nested/",
            "image/jpeg",
            true),
        new(
            "avc-dcim-v1",
            "dcs-diag-v1-avc-dcim.mp4",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Video,
            "DCIM/DcimCloudSyncDiagnostic/",
            "video/mp4",
            false),
        new(
            "hevc-main10-dcim-v1",
            "dcs-diag-v1-hevc10-dcim.mp4",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Video,
            "DCIM/DcimCloudSyncDiagnostic/",
            "video/mp4",
            true),
        new(
            "avc-large-cancellation-dcim-v1",
            "dcs-diag-v1-cancel-large-dcim.mp4",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Video,
            "DCIM/DcimCloudSyncDiagnostic/",
            "video/mp4",
            false),
        new(
            "jpeg-pictures-control-v1",
            "dcs-diag-v1-jpeg-pictures-control.jpg",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Image,
            "Pictures/DcimCloudSyncDiagnostic/",
            "image/jpeg",
            false),
        new(
            "avc-movies-control-v1",
            "dcs-diag-v1-avc-movies-control.mp4",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Video,
            "Movies/DcimCloudSyncDiagnostic/",
            "video/mp4",
            false),
        new(
            "jpeg-dcim-like-control-v1",
            "dcs-diag-v1-jpeg-dcim-like-control.jpg",
            Pixel7DiagnosticReport.DiagnosticMediaKind.Image,
            "DCIM-like/DcimCloudSyncDiagnostic/",
            "image/jpeg",
            false),
    ];

    private DiagnosticFixtureManifest(string fixtureSetVersion, IReadOnlyList<DiagnosticFixture> fixtures)
    {
        FixtureSetVersion = fixtureSetVersion;
        Fixtures = fixtures;
    }

    /// <summary>Gets the validated deterministic fixture-set version.</summary>
    internal string FixtureSetVersion { get; }

    /// <summary>Gets immutable validated fixtures, including internal query-only identity metadata.</summary>
    internal IReadOnlyList<DiagnosticFixture> Fixtures { get; }

    /// <summary>Loads and strictly validates the packaged synthetic fixture manifest.</summary>
    /// <param name="cancellationToken">The token that cancels packaged manifest loading.</param>
    /// <returns>A task whose result is the validated internal manifest.</returns>
    /// <exception cref="InvalidDataException">Thrown when the packaged manifest is malformed or unsupported.</exception>
    /// <exception cref="OperationCanceledException">Thrown when loading is canceled.</exception>
    internal static async Task<DiagnosticFixtureManifest> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(AssetName);
        cancellationToken.ThrowIfCancellationRequested();

        ManifestDocument? document;

        try
        {
            document = await JsonSerializer.DeserializeAsync<ManifestDocument>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("The packaged diagnostic fixture manifest is not valid JSON.");
        }

        return Validate(document);
    }

    private static DiagnosticFixtureManifest Validate(ManifestDocument? document)
    {
        if (document is null)
        {
            throw new InvalidDataException("The packaged diagnostic fixture manifest is empty.");
        }

        if (document.ManifestVersion != SupportedManifestVersion)
        {
            throw new InvalidDataException("The packaged diagnostic fixture manifest version is unsupported.");
        }

        if (!string.Equals(
            document.FixtureSetVersion,
            SupportedFixtureSetVersion,
            StringComparison.Ordinal))
        {
            throw new InvalidDataException("The packaged diagnostic fixture-set version is unsupported.");
        }

        if (document.Fixtures is not { Count: > 0 })
        {
            throw new InvalidDataException("The packaged diagnostic fixture manifest contains no fixtures.");
        }

        if (document.Fixtures.Count != AllowedFixtures.Length)
        {
            throw new InvalidDataException("The packaged diagnostic fixture manifest is missing or adds fixtures.");
        }

        HashSet<string> fixtureIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> displayNames = new(StringComparer.OrdinalIgnoreCase);
        List<DiagnosticFixture> fixtures = new(document.Fixtures.Count);
        var selectedFixtureCount = 0;

        for (var index = 0; index < document.Fixtures.Count; index++)
        {
            var fixtureDocument = document.Fixtures[index];

            if (string.IsNullOrWhiteSpace(fixtureDocument.Id)
                || string.IsNullOrWhiteSpace(fixtureDocument.DisplayName))
            {
                throw new InvalidDataException($"Fixture at index {index} has an empty identity.");
            }

            if (ContainsForbiddenIdentityMarker(fixtureDocument.Id)
                || ContainsForbiddenIdentityMarker(fixtureDocument.DisplayName))
            {
                throw new InvalidDataException($"Fixture at index {index} has a wildcard or placeholder identity.");
            }

            if (!fixtureIds.Add(fixtureDocument.Id))
            {
                throw new InvalidDataException("Diagnostic fixture IDs must be unique case-insensitively.");
            }

            if (!displayNames.Add(fixtureDocument.DisplayName))
            {
                throw new InvalidDataException("Diagnostic fixture display names must be unique case-insensitively.");
            }

            var expected = AllowedFixtures.FirstOrDefault(
                candidate => string.Equals(candidate.Id, fixtureDocument.Id, StringComparison.Ordinal));

            if (expected is null
                || !string.Equals(expected.DisplayName, fixtureDocument.DisplayName, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Fixture at index {index} has an unsupported deterministic identity.");
            }

            var fixture = ValidateFixture(fixtureDocument, expected, index);
            fixtures.Add(fixture);

            if (fixture.SelectedForLimitedAccess)
            {
                selectedFixtureCount++;
            }
        }

        if (selectedFixtureCount == 0)
        {
            throw new InvalidDataException("The selected-access fixture subset must not be empty.");
        }

        return new DiagnosticFixtureManifest(
            SupportedFixtureSetVersion,
            Array.AsReadOnly(fixtures.ToArray()));
    }

    private static DiagnosticFixture ValidateFixture(
        FixtureDocument document,
        ExpectedFixtureDefinition expected,
        int index)
    {
        var mediaKind = document.MediaKind switch
        {
            "image" => Pixel7DiagnosticReport.DiagnosticMediaKind.Image,
            "video" => Pixel7DiagnosticReport.DiagnosticMediaKind.Video,
            _ => throw new InvalidDataException($"Fixture at index {index} has an unsupported media kind."),
        };

        if (mediaKind != expected.MediaKind)
        {
            throw new InvalidDataException($"Fixture at index {index} has an inconsistent media kind.");
        }

        if (!string.Equals(
            document.ExpectedRelativePlacement,
            expected.ExpectedRelativePlacement,
            StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Fixture at index {index} has an unsupported relative placement.");
        }

        if (!string.Equals(document.ExpectedMimeType, expected.ExpectedMimeType, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Fixture at index {index} has an inconsistent MIME type.");
        }

        if (document.ExpectedByteCount is not > 0)
        {
            throw new InvalidDataException($"Fixture at index {index} has a non-positive byte count.");
        }

        if (!IsLowercaseSha256(document.ExpectedSha256))
        {
            throw new InvalidDataException($"Fixture at index {index} has a malformed SHA-256 digest.");
        }

        if (document.SelectedForLimitedAccess != expected.SelectedForLimitedAccess)
        {
            throw new InvalidDataException($"Fixture at index {index} has an inconsistent selected-access marker.");
        }

        var visibility = ValidateVisibility(document, mediaKind, index);

        return new DiagnosticFixture(
            expected.Id,
            expected.DisplayName,
            mediaKind,
            expected.ExpectedRelativePlacement,
            expected.ExpectedMimeType,
            document.ExpectedByteCount.Value,
            document.ExpectedSha256!,
            expected.SelectedForLimitedAccess,
            visibility);
    }

    private static DiagnosticFixtureVisibility ValidateVisibility(
        FixtureDocument document,
        Pixel7DiagnosticReport.DiagnosticMediaKind mediaKind,
        int index)
    {
        if (document.ExpectedVisibility is null)
        {
            throw new InvalidDataException($"Fixture at index {index} has no scenario visibility contract.");
        }

        var expectedImagesOnly = mediaKind == Pixel7DiagnosticReport.DiagnosticMediaKind.Image;
        var expectedVideosOnly = mediaKind == Pixel7DiagnosticReport.DiagnosticMediaKind.Video;
        var expectedSelectedOnly = document.SelectedForLimitedAccess == true;

        if (document.ExpectedVisibility.Full != true
            || document.ExpectedVisibility.ImagesOnly != expectedImagesOnly
            || document.ExpectedVisibility.VideosOnly != expectedVideosOnly
            || document.ExpectedVisibility.SelectedOnly != expectedSelectedOnly
            || document.ExpectedVisibility.Denied != false)
        {
            throw new InvalidDataException($"Fixture at index {index} has inconsistent scenario visibility.");
        }

        return new DiagnosticFixtureVisibility(
            Full: true,
            ImagesOnly: expectedImagesOnly,
            VideosOnly: expectedVideosOnly,
            SelectedOnly: expectedSelectedOnly,
            Denied: false);
    }

    private static bool ContainsForbiddenIdentityMarker(string value)
    {
        return value.IndexOfAny(['*', '?', '[', ']', '{', '}', '<', '>']) >= 0
            || value.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
            || value.Contains("changeme", StringComparison.OrdinalIgnoreCase)
            || value.Contains("todo", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLowercaseSha256(string? value)
    {
        return value is { Length: 64 }
            && value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    /// <summary>Represents one validated internal synthetic fixture.</summary>
    internal sealed record DiagnosticFixture(
        string Id,
        string DisplayName,
        Pixel7DiagnosticReport.DiagnosticMediaKind MediaKind,
        string ExpectedRelativePlacement,
        string ExpectedMimeType,
        long ExpectedByteCount,
        string ExpectedSha256,
        bool SelectedForLimitedAccess,
        DiagnosticFixtureVisibility ExpectedVisibility);

    /// <summary>Represents validated permission visibility retained only inside the diagnostic implementation.</summary>
    internal sealed record DiagnosticFixtureVisibility(
        bool Full,
        bool ImagesOnly,
        bool VideosOnly,
        bool SelectedOnly,
        bool Denied);

    private sealed record ManifestDocument
    {
        [JsonRequired]
        public int ManifestVersion { get; init; }

        [JsonRequired]
        public string? FixtureSetVersion { get; init; }

        [JsonRequired]
        public List<FixtureDocument>? Fixtures { get; init; }
    }

    private sealed record FixtureDocument
    {
        [JsonRequired]
        public string? Id { get; init; }

        [JsonRequired]
        public string? DisplayName { get; init; }

        [JsonRequired]
        public string? MediaKind { get; init; }

        [JsonRequired]
        public string? ExpectedRelativePlacement { get; init; }

        [JsonRequired]
        public string? ExpectedMimeType { get; init; }

        [JsonRequired]
        public long? ExpectedByteCount { get; init; }

        [JsonRequired]
        public string? ExpectedSha256 { get; init; }

        [JsonRequired]
        public bool? SelectedForLimitedAccess { get; init; }

        [JsonRequired]
        public VisibilityDocument? ExpectedVisibility { get; init; }
    }

    private sealed record VisibilityDocument
    {
        [JsonRequired]
        public bool? Full { get; init; }

        [JsonRequired]
        public bool? ImagesOnly { get; init; }

        [JsonRequired]
        public bool? VideosOnly { get; init; }

        [JsonRequired]
        public bool? SelectedOnly { get; init; }

        [JsonRequired]
        public bool? Denied { get; init; }
    }

    private sealed record ExpectedFixtureDefinition(
        string Id,
        string DisplayName,
        Pixel7DiagnosticReport.DiagnosticMediaKind MediaKind,
        string ExpectedRelativePlacement,
        string ExpectedMimeType,
        bool SelectedForLimitedAccess);
}
