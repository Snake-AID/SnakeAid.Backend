using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Domains;

public class AuthSession : BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string RefreshTokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime RefreshTokenExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(200)]
    public string? RevokedReason { get; set; }

    public Account? User { get; set; }
}
