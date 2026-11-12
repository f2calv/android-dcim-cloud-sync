namespace DcimCloudSync.Exceptions;

/// <summary>
/// Reports that a backup destination failed for a reason other than an existing backup.
/// </summary>
/// <remarks>Messages name the operation and status only, never item names, paths, endpoints or credentials.</remarks>
public class BackupDestinationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackupDestinationException"/> class.
    /// </summary>
    /// <param name="message">A message free of item names, paths, endpoints and credentials.</param>
    /// <param name="innerException">The provider failure.</param>
    public BackupDestinationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
