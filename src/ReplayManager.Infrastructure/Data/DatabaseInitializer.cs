using Microsoft.EntityFrameworkCore;
using ReplayManager.Domain.Entities;

namespace ReplayManager.Infrastructure.Data;

/// <summary>
/// Responsável pela inicialização segura do banco de dados SQLite, configuração de WAL mode
/// e semeadura de dados padrão do sistema.
/// </summary>
public class DatabaseInitializer
{
    private readonly ReplayManagerDbContext _context;

    public DatabaseInitializer(ReplayManagerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Inicializa o banco de dados garantindo esquema, ativando WAL e populando padrões.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Garante a existência do arquivo e tabelas
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        // Configura pragmas de integridade e alta performance para concorrência desktop
        await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken);

        await SeedDefaultSettingsAndTemplatesAsync(cancellationToken);
    }

    private async Task SeedDefaultSettingsAndTemplatesAsync(CancellationToken cancellationToken)
    {
        // Modelo padrão de descrição
        if (!await _context.DescriptionTemplates.AnyAsync(t => t.IsDefault, cancellationToken))
        {
            var defaultTemplate = new DescriptionTemplate
            {
                Name = "Padrão PUBG Replay",
                TemplateContent = "PUBG Replay\nData: {Date} {Time}\nDuração: {Duration}\nArquivo: {OriginalFileName}\nTamanho: {FileSize}\nGravado em: {FolderName}",
                IsDefault = true
            };
            _context.DescriptionTemplates.Add(defaultTemplate);
        }

        // Configurações padrão do sistema
        var defaultSettings = new List<AppSetting>
        {
            new() { Key = "CloseAction", Value = "Prompt", Description = "Ação ao fechar janela: Prompt, MinimizeToTray, CloseWindowKeepBackground, Terminate" },
            new() { Key = "StartWithWindows", Value = "false", Description = "Iniciar o aplicativo minimizado ao ligar o Windows" },
            new() { Key = "UploadBandwidthThrottlePercent", Value = "50", Description = "Percentual padrão de banda de upload a utilizar (50%)" },
            new() { Key = "MaxConcurrentUploads", Value = "1", Description = "Número máximo de uploads simultâneos (estritamente 1 na v1)" },
            new() { Key = "DefaultPrivacy", Value = "Unlisted", Description = "Privacidade padrão para novos uploads no YouTube" }
        };

        foreach (var setting in defaultSettings)
        {
            if (!await _context.AppSettings.AnyAsync(s => s.Key == setting.Key, cancellationToken))
            {
                _context.AppSettings.Add(setting);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
