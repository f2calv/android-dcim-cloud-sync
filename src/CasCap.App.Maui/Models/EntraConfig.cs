using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>
/// Configures Microsoft Entra sign-in for uploads from the device.
/// </summary>
/// <remarks>
/// The values are not secrets, but they identify the owner's directory and app registration, so they are entered on the
/// device and never packaged or committed.
/// </remarks>
public sealed record EntraConfig
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "Entra";

    /// <summary>Gets the directory (tenant) ID that owns the storage account, or <see langword="null"/> when not set.</summary>
    [RegularExpression("^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$")]
    public string? TenantId { get; set; }

    /// <summary>Gets the application (client) ID of the public client app registration, or <see langword="null"/> when not set.</summary>
    [RegularExpression("^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$")]
    public string? ClientId { get; set; }

    /// <summary>Gets a value indicating whether both identifiers are set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(ClientId);
}
