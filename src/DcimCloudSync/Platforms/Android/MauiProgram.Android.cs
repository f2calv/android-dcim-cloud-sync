using DcimCloudSync.Services;

namespace DcimCloudSync;

public static partial class MauiProgram
{
    static partial void AddPlatformServices(MauiAppBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IMediaAccess, AndroidMediaAccess>();
        builder.Services.AddSingleton<IMediaSource, MediaStoreMediaSource>();
        builder.Services.AddSingleton<MediaInventoryService>();
    }
}
