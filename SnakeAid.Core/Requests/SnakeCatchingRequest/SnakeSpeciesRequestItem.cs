using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakeCatchingRequest
{
    public class SnakeSpeciesRequestItem
    {
        [Required]
        public int SnakeSpeciesId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
    }
}
