namespace DcimCloudSync.Diagnostics;

/// <summary>
/// Represents an immutable Android-free report from one temporary Pixel 7 MediaStore diagnostic run.
/// </summary>
/// <remarks>
/// Reports are foreground-only and synthetic-only. They contain fixture IDs, aggregate values, hashes,
/// durations, status codes, and descriptor traits, but never names, paths, volumes, URIs, media metadata,
/// platform handles, media bytes, or exceptions.
/// </remarks>
public sealed record Pixel7DiagnosticReport
{
    /// <summary>Initializes a sanitized report for one synthetic diagnostic run.</summary>
    /// <param name="resultCode">The aggregate outcome of the run.</param>
    /// <param name="environment">The sanitized runtime environment summary.</param>
    /// <param name="access">The access snapshot used by the run.</param>
    /// <param name="scenarioExpectations">The manifest-derived permission scenario expectations.</param>
    /// <param name="discoveryObservations">The aggregate discovery observations.</param>
    /// <param name="fixtureObservations">The per-fixture sanitized observations.</param>
    /// <param name="duration">The elapsed run duration.</param>
    public Pixel7DiagnosticReport(
        DiagnosticResultCode resultCode,
        DiagnosticEnvironmentSummary environment,
        MediaAccessDiagnosticSnapshot access,
        IEnumerable<PermissionScenarioExpectation> scenarioExpectations,
        IEnumerable<AggregateDiscoveryObservation> discoveryObservations,
        IEnumerable<FixtureDiagnosticObservation> fixtureObservations,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(scenarioExpectations);
        ArgumentNullException.ThrowIfNull(discoveryObservations);
        ArgumentNullException.ThrowIfNull(fixtureObservations);

        ResultCode = resultCode;
        Environment = environment;
        Access = access;
        ScenarioExpectations = Array.AsReadOnly(scenarioExpectations.ToArray());
        DiscoveryObservations = Array.AsReadOnly(discoveryObservations.ToArray());
        FixtureObservations = Array.AsReadOnly(fixtureObservations.ToArray());
        Duration = duration;
    }

    /// <summary>Gets the aggregate outcome of the run.</summary>
    public DiagnosticResultCode ResultCode { get; }

    /// <summary>Gets the sanitized runtime environment summary.</summary>
    public DiagnosticEnvironmentSummary Environment { get; }

    /// <summary>Gets the access snapshot used by the run.</summary>
    public MediaAccessDiagnosticSnapshot Access { get; }

    /// <summary>Gets immutable manifest-derived permission scenario expectations.</summary>
    public IReadOnlyList<PermissionScenarioExpectation> ScenarioExpectations { get; }

    /// <summary>Gets immutable aggregate discovery observations.</summary>
    public IReadOnlyList<AggregateDiscoveryObservation> DiscoveryObservations { get; }

    /// <summary>Gets immutable per-fixture observations.</summary>
    public IReadOnlyList<FixtureDiagnosticObservation> FixtureObservations { get; }

    /// <summary>Gets the elapsed duration of the foreground diagnostic run.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Represents a sanitized runtime environment summary.</summary>
    public sealed record DiagnosticEnvironmentSummary(
        int RuntimeApiLevel,
        int TargetApiLevel,
        string PlatformVersion,
        string SecurityPatchMonth,
        string? MediaProviderModuleVersion,
        string FixtureSetVersion);

    /// <summary>Represents the expected visible synthetic fixture IDs for one permission scenario.</summary>
    public sealed record PermissionScenarioExpectation
    {
        /// <summary>Initializes an immutable permission-scenario expectation.</summary>
        /// <param name="scenario">The neutral permission scenario.</param>
        /// <param name="expectedVisibleFixtureIds">The expected visible synthetic fixture IDs.</param>
        public PermissionScenarioExpectation(
            VisualMediaPermissionScenario scenario,
            IEnumerable<string> expectedVisibleFixtureIds)
        {
            ArgumentNullException.ThrowIfNull(expectedVisibleFixtureIds);

            Scenario = scenario;
            ExpectedVisibleFixtureIds = Array.AsReadOnly(expectedVisibleFixtureIds.ToArray());
        }

        /// <summary>Gets the neutral permission scenario.</summary>
        public VisualMediaPermissionScenario Scenario { get; }

        /// <summary>Gets immutable expected visible synthetic fixture IDs.</summary>
        public IReadOnlyList<string> ExpectedVisibleFixtureIds { get; }
    }

    /// <summary>Represents sanitized aggregate discovery counts for one media kind.</summary>
    public sealed record AggregateDiscoveryObservation(
        DiagnosticMediaKind MediaKind,
        DiagnosticResultCode ResultCode,
        int ExpectedVisibleFixtureCount,
        int ActualVisibleFixtureCount,
        int ExpectedDcimFixtureCount,
        int ActualDcimFixtureCount,
        int UnexpectedControlFixtureCount,
        TimeSpan Duration);

    /// <summary>Represents sanitized discovery and open-path observations for one synthetic fixture ID.</summary>
    public sealed record FixtureDiagnosticObservation
    {
        /// <summary>Initializes an immutable per-fixture diagnostic observation.</summary>
        /// <param name="fixtureId">The deterministic synthetic fixture ID.</param>
        /// <param name="mediaKind">The neutral media kind.</param>
        /// <param name="discoveryResultCode">The sanitized discovery outcome.</param>
        /// <param name="openPathObservations">The sanitized open-path observations.</param>
        public FixtureDiagnosticObservation(
            string fixtureId,
            DiagnosticMediaKind mediaKind,
            DiagnosticResultCode discoveryResultCode,
            IEnumerable<MediaOpenPathObservation> openPathObservations)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
            ArgumentNullException.ThrowIfNull(openPathObservations);

            FixtureId = fixtureId;
            MediaKind = mediaKind;
            DiscoveryResultCode = discoveryResultCode;
            OpenPathObservations = Array.AsReadOnly(openPathObservations.ToArray());
        }

        /// <summary>Gets the deterministic synthetic fixture ID.</summary>
        public string FixtureId { get; }

        /// <summary>Gets the neutral media kind.</summary>
        public DiagnosticMediaKind MediaKind { get; }

        /// <summary>Gets the sanitized discovery outcome.</summary>
        public DiagnosticResultCode DiscoveryResultCode { get; }

        /// <summary>Gets immutable sanitized open-path observations.</summary>
        public IReadOnlyList<MediaOpenPathObservation> OpenPathObservations { get; }
    }

    /// <summary>Represents one sanitized synthetic-fixture open-path observation.</summary>
    public sealed record MediaOpenPathObservation(
        DiagnosticMediaOpenPath OpenPath,
        DiagnosticResultCode ResultCode,
        long ExpectedByteCount,
        long? ActualByteCount,
        string ExpectedSha256,
        string? ActualSha256,
        DiagnosticDescriptorTraits DescriptorTraits,
        TimeSpan Duration);

    /// <summary>Represents non-identifying stream and descriptor traits observed during an open path.</summary>
    public sealed record DiagnosticDescriptorTraits(long ReportedLength, bool CanSeek);

    /// <summary>Identifies a platform-neutral visual-media permission scenario.</summary>
    public enum VisualMediaPermissionScenario
    {
        /// <summary>Both broad image and broad video access are held.</summary>
        Full,

        /// <summary>Only broad image access is held.</summary>
        ImagesOnly,

        /// <summary>Only broad video access is held.</summary>
        VideosOnly,

        /// <summary>Only user-selected visual media is accessible.</summary>
        SelectedOnly,

        /// <summary>No visual-media access is held.</summary>
        Denied,

        /// <summary>Broad access for one kind is combined with selected access.</summary>
        Mixed,
    }

    /// <summary>Identifies a neutral synthetic media kind.</summary>
    public enum DiagnosticMediaKind
    {
        /// <summary>The fixture is an image.</summary>
        Image,

        /// <summary>The fixture is a video.</summary>
        Video,
    }

    /// <summary>Identifies one independent media-open path used by the temporary diagnostic.</summary>
    public enum DiagnosticMediaOpenPath
    {
        /// <summary>Uses the ordinary input-stream control path.</summary>
        Ordinary,

        /// <summary>Uses the require-original strict typed-open candidate path.</summary>
        StrictTyped,

        /// <summary>Uses the require-original original-format file-descriptor control path.</summary>
        OriginalFormatFileDescriptor,
    }

    /// <summary>Identifies every sanitized outcome produced by the temporary diagnostic boundary.</summary>
    public enum DiagnosticResultCode
    {
        /// <summary>The operation completed successfully.</summary>
        Success,

        /// <summary>Required visual-media access was denied.</summary>
        AccessDenied,

        /// <summary>Only selected, one-kind, or mixed visual-media access was available.</summary>
        PartialAccess,

        /// <summary>The requested permission transition is unsupported on the runtime API.</summary>
        UnsupportedTransition,

        /// <summary>The operation was canceled.</summary>
        Canceled,

        /// <summary>The requested media collection was unavailable.</summary>
        CollectionUnavailable,

        /// <summary>The system media provider was unavailable.</summary>
        ProviderUnavailable,

        /// <summary>The provider returned missing or inconsistent MIME metadata.</summary>
        InvalidMimeMetadata,

        /// <summary>The expected synthetic fixture was not found.</summary>
        FixtureNotFound,

        /// <summary>The original representation was unsupported for the requested open path.</summary>
        OriginalFormatUnsupported,

        /// <summary>The provider failed while querying or reading media.</summary>
        ProviderIoFailure,

        /// <summary>The packaged synthetic fixture contract was incomplete or malformed.</summary>
        MalformedFixture,

        /// <summary>The observed SHA-256 did not match the trusted fixture digest.</summary>
        HashMismatch,

        /// <summary>The observed byte count did not match the trusted fixture count.</summary>
        ByteCountMismatch,

        /// <summary>An indexed non-DCIM control unexpectedly passed the DCIM boundary.</summary>
        UnexpectedControlFixture,
    }
}
