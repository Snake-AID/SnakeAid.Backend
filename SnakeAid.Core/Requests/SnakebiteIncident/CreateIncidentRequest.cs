using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakebiteIncident
{
    public class CreateIncidentRequest
    {
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
    }
}
