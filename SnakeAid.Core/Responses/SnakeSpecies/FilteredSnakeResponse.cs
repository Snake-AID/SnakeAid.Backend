using System.Collections.Generic;

namespace SnakeAid.Core.Responses.SnakeSpecies
{
    public class FilteredSnakeResponse
    {
        public int Id { get; set; }
        public string ScientificName { get; set; }
        public string CommonName { get; set; }
        public string ImageUrl { get; set; }
        public bool IsVenomous { get; set; }
        public float RiskLevel { get; set; }
        
        /// <summary>
        /// Number of matched filter options
        /// </summary>
        public int MatchScore { get; set; }
        
        /// <summary>
        /// Total number of questions user answered
        /// </summary>
        public int TotalAnswered { get; set; }
        
        /// <summary>
        /// Match percentage (MatchScore / TotalAnswered * 100)
        /// </summary>
        public double MatchPercentage { get; set; }
        
        /// <summary>
        /// List of matched feature descriptions
        /// Example: ["Đầu tam giác", "Màu xanh lá", "Đuôi đỏ"]
        /// </summary>
        public List<string> MatchedFeatures { get; set; } = new List<string>();
    }
}
