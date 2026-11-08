using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReplayManager.Application.Contracts.Backup;
using ReplayManager.Application.Contracts.Export;
using ReplayManager.Application.Contracts.FileSystem;
using ReplayManager.Application.Contracts.Filters;
using ReplayManager.Application.Contracts.Ingestion;
using ReplayManager.Application.Contracts.Media;
using ReplayManager.Application.Contracts.Persistence;
using ReplayManager.Application.Contracts.SystemIntegration;
using ReplayManager.Application.Contracts.Upload;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Application.Services;
using ReplayManager.Application.Services.Upload;
using ReplayManager.Infrastructure.Background;
using ReplayManager.Infrastructure.Backup;
using ReplayManager.Infrastructure.Data;
using ReplayManager.Infrastructure.Export;
using ReplayManager.Infrastructure.FileSystem;
using ReplayManager.Infrastructure.FFmpeg;
using ReplayManager.Infrastructure.Repositories;
using ReplayManager.Infrastructure.Security;
using ReplayManager.Infrastructure.SystemIntegration;
using ReplayManager.Infrastructure.YouTube;

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
        services.AddSingleton<IFilterEvaluator, FilterEvaluator>();
        services.AddScoped<IVideoMediaProcessor, VideoMediaProcessor>();

        return services;
    }

    public static IServiceCollection AddInfrastructureYouTube(this IServiceCollection services)
    {
        services.AddSingleton<DpapiCredentialStorage>();
        services.AddSingleton<ITitleFormatter, TitleFormatter>();
        services.AddSingleton<IDescriptionTemplateEngine, DescriptionTemplateEngine>();
        services.AddSingleton<YouTubeAuthService>();
        services.AddSingleton<IYouTubeAuthService>(sp => sp.GetRequiredService<YouTubeAuthService>());
        services.AddScoped<IYouTubeUploader, YouTubeUploader>();

        // Serviços da Fila de Upload
        services.AddSingleton<IUploadQueueService, UploadQueueService>();
        services.AddScoped<IUploadQueueProcessor, UploadQueueProcessor>();
        services.AddHostedService<UploadQueueBackgroundService>();

        // Serviços de Backup (Fase 8)
        services.AddScoped<ISqliteBackupService, SqliteBackupService>();
        services.AddScoped<IGoogleDriveBackupService, GoogleDriveBackupService>();

        // Serviços de Sistema e Exportação (Fase 9)
        services.AddSingleton<IStartupRegistrationService, WindowsStartupRegistrationService>();
        services.AddScoped<ILibraryExportService, LibraryExportService>();

        return services;
    }
}
