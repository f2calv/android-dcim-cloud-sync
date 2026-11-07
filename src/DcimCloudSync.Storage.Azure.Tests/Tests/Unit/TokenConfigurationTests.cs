using Azure.Core;
using System.ComponentModel.DataAnnotations;

namespace DcimCloudSync.Tests.Unit;

/// <summary>
/// Tests token-based configuration of <see cref="AzureBlobBackupDestination"/>; no storage is contacted.
/// </summary>
[Trait("Category", "Configuration")]
public sealed class TokenConfigurationTests
{
    private static readonly Uri ServiceUri = new("https://account.blob.core.windows.net/");

    [Theory]
    [InlineData("UseDevelopmentStorage=true", null, true)]
    [InlineData(null, "https://account.blob.core.windows.net/", true)]
    [InlineData(null, null, false)]
    [InlineData("UseDevelopmentStorage=true", "https://account.blob.core.windows.net/", false)]
    [InlineData(null, "http://account.blob.core.windows.net/", false)]
    [InlineData(null, "https://account.blob.core.windows.net/media", false)]
    public void Validate(string? connectionString, string? blobServiceUri, bool valid)
    {
        var config = new AzureBlobBackupConfig
        {
            ConnectionString = connectionString,
            BlobServiceUri = blobServiceUri is null ? null : new Uri(blobServiceUri),
        };

        Assert.Equal(valid, Validator.TryValidateObject(config, new ValidationContext(config), null, validateAllProperties: true));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void IsConfigured_BlobServiceUri(bool tokenProviderRegistered, bool expected)
    {
        var destination = new AzureBlobBackupDestination(
            new StaticOptionsMonitor<AzureBlobBackupConfig>(new AzureBlobBackupConfig { BlobServiceUri = ServiceUri }),
            tokenProviderRegistered ? new FixedTokenProvider() : null);

        Assert.Equal(expected, destination.IsConfigured);
    }

    [Fact]
    public void IsConfigured_Unset() =>
        Assert.False(new AzureBlobBackupDestination(new StaticOptionsMonitor<AzureBlobBackupConfig>(new AzureBlobBackupConfig())).IsConfigured);

    [Fact]
    public async Task AccessTokenProviderCredential_GetTokenAsync()
    {
        var provider = new FixedTokenProvider();
        var credential = new AccessTokenProviderCredential(provider);

        var token = await credential.GetTokenAsync(
            new TokenRequestContext(["https://storage.azure.com/.default"]),
            TestContext.Current.CancellationToken);

        Assert.Equal((FixedTokenProvider.Token, FixedTokenProvider.ExpiresOn), (token.Token, token.ExpiresOn));
        Assert.Equal(["https://storage.azure.com/.default"], provider.RequestedScopes);
    }

    private sealed class FixedTokenProvider : IAccessTokenProvider
    {
        public const string Token = "synthetic-token";

        public static readonly DateTimeOffset ExpiresOn = DateTimeOffset.UnixEpoch.AddDays(1);

        public IReadOnlyList<string> RequestedScopes { get; private set; } = [];

        public bool IsConfigured => true;

        public Task<AccessTokenResult> GetTokenAsync(IReadOnlyList<string> scopes, CancellationToken cancellationToken)
        {
            RequestedScopes = scopes;
            return Task.FromResult(new AccessTokenResult(Token, ExpiresOn));
        }

        public Task SignOutAsync() => Task.CompletedTask;
    }
}
