namespace ReplayManager.Application.Contracts.SystemIntegration;

/// <summary>
/// Contrato para registro e controle de inicialização automática do aplicativo junto ao Windows.
/// </summary>
public interface IStartupRegistrationService
{
    /// <summary>
    /// Verifica se a inicialização automática com o Windows está habilitada.
    /// </summary>
    bool IsAutoStartEnabled();

    /// <summary>
    /// Ativa ou desativa a inicialização automática com o Windows.
    /// </summary>
    /// <param name="enable">True para habilitar, False para desabilitar.</param>
    /// <param name="arguments">Argumentos opcionais (ex: --minimized).</param>
    /// <returns>True se a alteração foi aplicada com sucesso.</returns>
    bool SetAutoStart(bool enable, string arguments = "--minimized");
}

