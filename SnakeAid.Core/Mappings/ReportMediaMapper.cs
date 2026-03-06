using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.SymptomConfig;

namespace SnakeAid.Core.Mappings
{
    public class ReportMediaMapper : IRegister
    {
        private const decimal MIN_CONFIDENCE_THRESHOLD = 0.3m; // 30% minimum confidence

        public void Register(TypeAdapterConfig config)
        {
            // ReportMedia → SnakeAIDetectMediaResponse (Simplified for Member/Rescuer)
            config.NewConfig<ReportMedia, SnakeAIDetectMediaResponse>()
                .Map(dest => dest.DetectedSpecies, src =>
                    src.AIRecognitionResults
                        // Filter 1: Only completed/verified results (no processing/failed)
                        .Where(r => r.Status == RecognitionStatus.Completed ||
                                    r.Status == RecognitionStatus.ExpertVerified)
                        // Filter 2: Only high-confidence results (>= 30%)
                        .Where(r => r.Confidence >= MIN_CONFIDENCE_THRESHOLD)
                        // Filter 3: Only results with mapped species (use FK instead of navigation property)
                        .Where(r => r.IsMapped && r.DetectedSpeciesId != null)
                        // Sort: LATEST first (if multiple AI runs), then HIGHEST confidence
                        .OrderByDescending(r => r.CreatedAt)
                        .ThenByDescending(r => r.Confidence)
                        // Deduplicate by species ID (keep first = latest + highest for each species)
                        .GroupBy(r => r.DetectedSpeciesId)
                        .Select(g => g.First().DetectedSpecies)
                        .ToList());

            // SnakeAIRecognitionResult → SnakeAIRecognitionResultResponse (for Admin/Expert)
            config.NewConfig<SnakeAIRecognitionResult, SnakeAIRecognitionResultResponse>()
                .Map(dest => dest.DetectedSpecies, src => src.DetectedSpecies);
        }
    }
}