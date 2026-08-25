using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BARANG.Core.Infrastructure;

internal sealed class DatabaseAvailabilityService(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseAvailabilityOptions> options,
    ILogger<DatabaseAvailabilityService> logger) : IHostedService
{
    private static readonly Action<ILogger, int, Exception?> DatabaseAvailable =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(1, nameof(DatabaseAvailable)),
            "PostgreSQL is available after {Attempt} attempt(s).");

    private static readonly Action<ILogger, int, int, Exception?> DatabaseUnavailable =
        LoggerMessage.Define<int, int>(
            LogLevel.Warning,
            new EventId(2, nameof(DatabaseUnavailable)),
            "PostgreSQL is unavailable (attempt {Attempt}/{MaximumAttempts}); retrying.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        DatabaseAvailabilityOptions settings = options.Value;
        if (!settings.WaitForAvailability)
        {
            return;
        }

        if (settings.MaximumAttempts < 1 || settings.DelaySeconds < 1)
        {
            throw new InvalidOperationException("Database availability retry settings must be positive.");
        }

        for (int attempt = 1; attempt <= settings.MaximumAttempts; attempt++)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            CoreDbContext dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            if (await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                DatabaseAvailable(logger, attempt, null);
                return;
            }

            if (attempt < settings.MaximumAttempts)
            {
                DatabaseUnavailable(logger, attempt, settings.MaximumAttempts, null);
                await Task.Delay(TimeSpan.FromSeconds(settings.DelaySeconds), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"PostgreSQL remained unavailable after {settings.MaximumAttempts} attempts.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
