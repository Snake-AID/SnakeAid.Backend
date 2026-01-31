namespace SnakeAid.Core.Requests
{
    public class PaginationRequest
    {
        /// <summary>
        /// Page number (starting from 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
