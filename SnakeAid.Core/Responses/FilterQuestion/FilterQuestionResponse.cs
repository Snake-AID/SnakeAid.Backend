using System.Collections.Generic;

namespace SnakeAid.Core.Responses.FilterQuestion
{
    public class FilterQuestionResponse
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public List<FilterOptionResponse> Options { get; set; } = new List<FilterOptionResponse>();
    }

    public class FilterOptionResponse
    {
        public int Id { get; set; }
        public string OptionText { get; set; }
        public string OptionImageUrl { get; set; }
    }
}
