using Microsoft.EntityFrameworkCore;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Infrastructure.Data;

/// <summary>
/// DbContext principal da aplicação para SQLite.
/// </summary>
public class ReplayManagerDbContext : DbContext
{
    public DbSet<MonitoredFolder> MonitoredFolders => Set<MonitoredFolder>();
    public DbSet<FolderFilterRule> FolderFilterRules => Set<FolderFilterRule>();
    public DbSet<VideoRecord> Videos => Set<VideoRecord>();
    public DbSet<UploadQueueItem> UploadQueue => Set<UploadQueueItem>();
    public DbSet<YouTubeAccount> YouTubeAccounts => Set<YouTubeAccount>();
    public DbSet<DescriptionTemplate> DescriptionTemplates => Set<DescriptionTemplate>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    public ReplayManagerDbContext(DbContextOptions<ReplayManagerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de MonitoredFolder
        modelBuilder.Entity<MonitoredFolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Path).IsUnique();

            entity.HasOne(e => e.FilterRule)
                  .WithOne(e => e.MonitoredFolder)
                  .HasForeignKey<FolderFilterRule>(e => e.MonitoredFolderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Videos)
                  .WithOne(e => e.MonitoredFolder)
                  .HasForeignKey(e => e.MonitoredFolderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DescriptionTemplates)
                  .WithOne(e => e.MonitoredFolder)
                  .HasForeignKey(e => e.MonitoredFolderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuração de FolderFilterRule
        modelBuilder.Entity<FolderFilterRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllowedExtensions).IsRequired().HasMaxLength(256).HasDefaultValue(".mp4;.mkv;.avi");
            entity.Property(e => e.MinSizeBytes).HasDefaultValue(10L * 1024L * 1024L);
            entity.Property(e => e.MaxSizeBytes).HasDefaultValue(100L * 1024L * 1024L);
            entity.Property(e => e.IsMaxSizeEnabled).HasDefaultValue(true);
            entity.Property(e => e.NamePatternRegex).HasMaxLength(512);
        });

        // Configuração de VideoRecord
        modelBuilder.Entity<VideoRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ContentSha256).IsRequired().HasMaxLength(64);
            entity.Property(e => e.YouTubeVideoId).HasMaxLength(64);
            entity.Property(e => e.YouTubeUrl).HasMaxLength(512);
            entity.Property(e => e.CustomTitle).HasMaxLength(100); // Limite YouTube v3: 100 caracteres
            entity.Property(e => e.CustomDescription).HasMaxLength(5000); // Limite YouTube v3: 5000 caracteres
            entity.Property(e => e.ThumbnailPath).HasMaxLength(1024);
            entity.Property(e => e.RejectionReason).HasMaxLength(1024);

            // REGRA FUNDAMENTAL DE DEDUPLICAÇÃO (Seção 5.1 e ADR 003):
            // Unicidade de hash restrita à mesma pasta monitorada.
            entity.HasIndex(e => new { e.MonitoredFolderId, e.ContentSha256 })
                  .IsUnique()
                  .HasDatabaseName("IX_Videos_MonitoredFolderId_ContentSha256");

            // Índices de busca e filtros rápidos na interface
            entity.HasIndex(e => e.UploadStatus);
            entity.HasIndex(e => e.LocalStatus);
            entity.HasIndex(e => e.FilterStatus);
            entity.HasIndex(e => e.RecordedAtUtc);
            entity.HasIndex(e => e.FileName);

            entity.HasOne(e => e.UploadQueueItem)
                  .WithOne(e => e.VideoRecord)
                  .HasForeignKey<UploadQueueItem>(e => e.VideoRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuração de UploadQueueItem
        modelBuilder.Entity<UploadQueueItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResumableSessionUri).HasMaxLength(2048);
            entity.Property(e => e.LastError).HasMaxLength(2048);

            // Índices para processamento sequencial prioritário
            entity.HasIndex(e => new { e.Status, e.Priority, e.QueuedAtUtc })
                  .HasDatabaseName("IX_UploadQueue_ProcessingOrder");
        });

        // Configuração de YouTubeAccount
        modelBuilder.Entity<YouTubeAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChannelId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ChannelTitle).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ChannelThumbnailUrl).HasMaxLength(1024);
            entity.Property(e => e.EncryptedAuthData).IsRequired();
            entity.HasIndex(e => e.ChannelId).IsUnique();
        });

        // Configuração de DescriptionTemplate
        modelBuilder.Entity<DescriptionTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.TemplateContent).IsRequired().HasMaxLength(5000);
        });

        // Configuração de AppSetting
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
