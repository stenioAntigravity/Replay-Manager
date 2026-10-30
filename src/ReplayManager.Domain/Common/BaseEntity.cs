namespace ReplayManager.Domain.Common;

/// <summary>
/// Classe base para todas as entidades persistentes do domínio.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
