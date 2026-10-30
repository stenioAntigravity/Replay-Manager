using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.Repositories;

namespace ReplayManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services,
        string sqliteDbPath)
    {
        var connectionString = $"Data Source={sqliteDbPath};";

        services.AddDbContext<ReplayManagerDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IVideoRecordRepository, VideoRecordRepository>();
        services.AddScoped<IMonitoredFolderRepository, MonitoredFolderRepository>();
        services.AddScoped<IUploadQueueRepository, UploadQueueRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
