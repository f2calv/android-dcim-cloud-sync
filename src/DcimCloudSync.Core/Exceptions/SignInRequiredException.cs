namespace DcimCloudSync.Exceptions;

/// <summary>
/// Reports that the destination needs the user to sign in, and no prompt could be shown.
/// </summary>
/// <remarks>Raised when no cached sign-in can be renewed while the app is in the background, or while prompts are suppressed
/// by <see cref="Services.InteractiveSignIn"/>.</remarks>
public sealed class SignInRequiredException : BackupDestinationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignInRequiredException"/> class.
    /// </summary>
    /// <param name="innerException">The sign-in failure that needs the user, if any.</param>
    public SignInRequiredException(Exception? innerException = null)
        : base("Sign-in is required. Open the app and back up again.", innerException ?? new InvalidOperationException("No interactive sign-in is possible."))
    {
    }
}
