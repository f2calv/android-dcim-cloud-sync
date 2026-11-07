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

/// <summary>
/// Classifies how much of the device's media the user allowed the app to read.
/// </summary>
public enum MediaAccessLevel
{
    /// <summary>No photos or videos are visible.</summary>
    Denied,

    /// <summary>Only the photos and videos the user selected are visible.</summary>
    Partial,

    /// <summary>All photos and videos are visible.</summary>
    Full,
}

/// <summary>
/// Selects how a backup run decides that an item is already backed up.
/// </summary>
public enum BackupCheckMode
{
    /// <summary>
    /// An existing backup whose recorded source size and modification time match the item counts as backed up without
    /// reading the item; other items are hashed and compared.
    /// </summary>
    Quick,

    /// <summary>Every item is read and its SHA-256 compared with the existing backup.</summary>
    Full,
}

/// <summary>
/// Names the step a backup run is performing on its current item.
/// </summary>
public enum BackupItemPhase
{
    /// <summary>Between items, or the run has finished.</summary>
    None,

    /// <summary>Looking up the item's existing backup.</summary>
    Checking,

    /// <summary>Reading the item to compute its SHA-256.</summary>
    Hashing,

    /// <summary>Uploading the item.</summary>
    Uploading,
}

/// <summary>
/// Classifies an entry in the backup log.
/// </summary>
public enum BackupLogLevel
{
    /// <summary>A routine event, such as an upload.</summary>
    Information,

    /// <summary>An item could not be backed up, but the run continued.</summary>
    Warning,

    /// <summary>The run stopped.</summary>
    Error,
}
