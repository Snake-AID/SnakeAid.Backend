using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.SnakeSpecies
{
    /// <summary>
    /// Request to get snakes by GPS location
    /// </summary>
    public class GetSnakesByLocationRequest
    {
        /// <summary>
        /// Latitude (WGS84)
        /// </summary>
        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Lat { get; set; }

        /// <summary>
        /// Longitude (WGS84)
        /// </summary>
        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Lng { get; set; }
    }
}
