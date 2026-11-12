namespace CasCap.Services;

/// <summary>
/// Marks the current asynchronous flow as unattended, so token providers never prompt for sign-in within it.
/// </summary>
/// <remarks>
/// Background runs use it so an expired sign-in fails with <see cref="Exceptions.SignInRequiredException"/> instead of opening
/// a browser over whatever the user is doing. The flag flows to awaited calls started inside the scope.
/// </remarks>
public static class InteractiveSignIn
{
    private static readonly AsyncLocal<bool> Suppressed = new();

    /// <summary>
    /// Gets a value indicating whether sign-in prompts are suppressed in the current flow.
    /// </summary>
    public static bool IsSuppressed => Suppressed.Value;

    /// <summary>
    /// Suppresses sign-in prompts in the current flow until the returned scope is disposed.
    /// </summary>
    /// <returns>A scope that restores the previous setting when disposed.</returns>
    public static IDisposable Suppress()
    {
        var previous = Suppressed.Value;
        Suppressed.Value = true;
        return new Scope(previous);
    }

    private sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => Suppressed.Value = previous;
    }
}
