using System;

namespace SnakeAid.Core.Responses.SnakeCatchingRequest
{
    public class CatchingRequestDetailResponse
    {
        public Guid Id { get; set; }
        public Guid SnakeCatchingRequestId { get; set; }
        public int SnakeSpeciesId { get; set; }
        public int Quantity { get; set; }
        public string? SnakeSpeciesName { get; set; }
        public string? SnakeSpeciesScientificName { get; set; }
    }
}
