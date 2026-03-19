using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Wallet;

public class CreateWalletTopupRequest
{
    [Required]
    [Range(1000, 10000000, ErrorMessage = "Top-up amount must be between 1,000 and 10,000,000 VND")]
    public int Amount { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}