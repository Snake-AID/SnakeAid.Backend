using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.SnakeCatchingRequest
{
    public class CancelSnakeCatchingRequestRequest
    {
        /// <summary>
        /// Reason for cancelling the request
        /// </summary>
        [Required(ErrorMessage = "Cancellation reason is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }
}
