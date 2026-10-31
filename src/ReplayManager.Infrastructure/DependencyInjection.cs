using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Contracts.Ingestion;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Services;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.FileSystem;
using ReplayManager.Infrastructure.FFmpeg;
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

    public static IServiceCollection AddInfrastructureFileSystem(this IServiceCollection services)
    {
        services.AddSingleton<IContentHasher, Sha256ContentHasher>();
        services.AddSingleton<IFileStabilityChecker, FileStabilityChecker>();
        services.AddSingleton<IFileWatcherService, FileSystemWatcherService>();
        services.AddScoped<IVideoIngestionService, VideoIngestionService>();
        services.AddScoped<IFolderReconciliationWorker, FolderReconciliationWorker>();

        return services;
    }

    public static IServiceCollection AddInfrastructureFFmpeg(this IServiceCollection services, string? customFFmpegDirectory = null)
    {
        services.AddSingleton<IFFmpegPathResolver>(sp =>
            new FFmpegPathResolver(
                sp.GetService<Microsoft.Extensions.Logging.ILogger<FFmpegPathResolver>>(),
                customFFmpegDirectory));

        services.AddSingleton<IMediaMetadataExtractor, FFprobeMetadataExtractor>();
        services.AddSingleton<IThumbnailGenerator, FFmpegThumbnailGenerator>();
        services.AddSingleton<ReplayManager.Application.Contracts.Filters.IFilterEvaluator, ReplayManager.Application.Services.FilterEvaluator>();
        services.AddScoped<IVideoMediaProcessor, VideoMediaProcessor>();

        return services;
    }
}
