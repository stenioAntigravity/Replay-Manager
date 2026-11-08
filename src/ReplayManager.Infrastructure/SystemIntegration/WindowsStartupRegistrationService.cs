using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ReplayManager.Application.Contracts.SystemIntegration;

namespace ReplayManager.Infrastructure.SystemIntegration;

/// <summary>
/// Implementação do serviço de inicialização automática via registro do Windows (HKCU\Software\Microsoft\Windows\CurrentVersion\Run).
/// Não requer privilégios de Administrador pois atua no escopo do usuário atual (HKCU).
/// </summary>
public class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string RunRegistrySubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApplicationRegistryName = "ReplayManager";

    private readonly ILogger<WindowsStartupRegistrationService>? _logger;
    private readonly string? _executablePathOverride;

    public WindowsStartupRegistrationService(
        ILogger<WindowsStartupRegistrationService>? logger = null,
        string? executablePathOverride = null)
    {
        _logger = logger;
        _executablePathOverride = executablePathOverride;
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: false);
            if (key == null) return false;

            var value = key.GetValue(ApplicationRegistryName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Falha ao verificar registro de inicialização com o Windows.");
            return false;
        }
    }

    public bool SetAutoStart(bool enable, string arguments = "--minimized")
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistrySubKey, writable: true);
            if (key == null)
            {
                _logger?.LogError("Chave de registro do Windows para inicialização automática não pôde ser aberta.");
                return false;
            }

            if (enable)
            {
                var exePath = _executablePathOverride ?? Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    _logger?.LogError("Não foi possível determinar o caminho do executável para inicialização automática.");
                    return false;
                }

                var formattedCommand = string.IsNullOrWhiteSpace(arguments)
                    ? $"\"{exePath}\""
                    : $"\"{exePath}\" {arguments.Trim()}";

                key.SetValue(ApplicationRegistryName, formattedCommand);
                _logger?.LogInformation("Inicialização automática com o Windows ativada: {Command}", formattedCommand);
            }
            else
            {
                key.DeleteValue(ApplicationRegistryName, throwOnMissingValue: false);
                _logger?.LogInformation("Inicialização automática com o Windows desativada.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erro ao alterar inicialização automática com o Windows.");
            return false;
        }
    }
}

