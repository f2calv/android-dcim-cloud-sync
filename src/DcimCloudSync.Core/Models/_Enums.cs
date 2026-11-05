namespace DcimCloudSync.Models;

/// <summary>
/// Classifies what happened to one media item during a backup run.
/// </summary>
public enum BackupItemOutcome
{
    /// <summary>The item was uploaded.</summary>
    Uploaded,

    /// <summary>A backup with the same SHA-256 already existed.</summary>
    AlreadyBackedUp,

    /// <summary>A backup with a different SHA-256 already existed; neither was changed.</summary>
    Conflict,

    /// <summary>A backup without a recorded SHA-256 already existed; it was left unchanged.</summary>
    Unverified,

    /// <summary>The item changed after hashing, so its upload was abandoned before commit; a later run retries it.</summary>
    Changed,
}
