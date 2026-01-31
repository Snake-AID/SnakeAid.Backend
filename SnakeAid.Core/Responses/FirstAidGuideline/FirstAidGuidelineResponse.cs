using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.FirstAidGuideline
{
    public class FirstAidGuidelineResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public GuidelineType Type { get; set; }
        public string? Summary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
