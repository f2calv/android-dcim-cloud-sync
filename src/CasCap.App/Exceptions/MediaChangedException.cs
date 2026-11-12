namespace CasCap.Exceptions;

/// <summary>
/// Reports that a media item's content changed between hashing it and backing it up.
/// </summary>
/// <remarks>Thrown at the end of the upload content, so a destination that honours its contract commits nothing.
/// The message names no item, path or hash.</remarks>
public sealed class MediaChangedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaChangedException"/> class.
    /// </summary>
    public MediaChangedException()
        : base("The media item changed while it was being backed up.")
    {
    }
}
