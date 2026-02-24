using SnakeAid.Core.Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakeCatchingRequest
{
    public class CreateSnakeCatchingRequestRequest
    {
        [Required]
        [MaxLength(1000)]
        public string Address { get; set; }

        /// <summary>
        /// Longitude (Kinh độ) - VD: 106.660172
        /// </summary>
        [Required]
        [Range(-180, 180)]
        public double Lng { get; set; }

        /// <summary>
        /// Latitude (Vĩ độ) - VD: 10.762622
        /// </summary>
        [Required]
        [Range(-90, 90)]
        public double Lat { get; set; }

        [MaxLength(2000)]
        public string AdditionalDetails { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Optional: List of snake species if user identified the snakes
        /// </summary>
        public List<SnakeSpeciesRequestItem> SnakeSpeciesList { get; set; } = new List<SnakeSpeciesRequestItem>();

        public List<string?> MediaURLList { get; set; } = new List<string>();
    }
}
