using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreetArtist.Core;

public sealed class StreetArtistCore : BackgroundService {
    public StreetArtistCore(
        ILogger<StreetArtistCore> logger
    ) {
        this.logger = logger;
    }

    #region Fields
    readonly ILogger logger;
    #endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("StreetArtistCore Started");
        while (!stoppingToken.IsCancellationRequested) {
            await Task.Delay(TimeSpan.FromMilliseconds(20));
            logger.LogInformation("StreetArtistCore Event Loop");
        }
        logger.LogInformation("StreetArtistCore Ended");
    }
}

public static class StreetArtistCore_Extensions {
    public static IServiceCollection AddStreetArtistCoreDefaultServices(this IServiceCollection services) {
        // Add Logging
        services.AddLogging(config => config
            .AddConsole()
            .AddDebug()
            .SetMinimumLevel(LogLevel.Debug));
        return services;
    }

    public static IServiceCollection AddStreetArtistCore(this IServiceCollection services) {
        // Add Services
        services.AddMediatR(config => {
            config.RegisterServicesFromAssembly(typeof(global::StreetArtist.Core.StreetArtistCore).Assembly);
        });

        services.AddHostedService<StreetArtistCore>();
        return services;
    }
}
