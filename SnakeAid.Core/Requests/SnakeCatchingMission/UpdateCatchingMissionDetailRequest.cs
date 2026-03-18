using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakeCatchingMission
{
    public class UpdateCatchingMissionDetailRequest
    {
        /// <summary>
        /// ID loài rắn
        /// </summary>
        [Required(ErrorMessage = "SnakeSpeciesId is required")]
        public int SnakeSpeciesId { get; set; }

        /// <summary>
        /// Số lượng rắn bắt được
        /// </summary>
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
