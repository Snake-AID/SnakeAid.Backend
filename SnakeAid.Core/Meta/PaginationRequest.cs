using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Meta
{
    public class PaginationRequest
    {
        /// <summary>
        /// Page number (starting from 1)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }
}
