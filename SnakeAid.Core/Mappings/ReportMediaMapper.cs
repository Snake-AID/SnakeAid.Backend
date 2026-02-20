using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;

namespace SnakeAid.Core.Mappings
{
    public class ReportMediaMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // ReportMedia → SnakeAIDetectMediaResponse
            config.NewConfig<ReportMedia, SnakeAIDetectMediaResponse>()
                .Map(dest => dest.AIRecognitionResults, src => src.AIRecognitionResults);

            // SnakeAIRecognitionResult → SnakeAIRecognitionResultResponse
            config.NewConfig<SnakeAIRecognitionResult, SnakeAIRecognitionResultResponse>()
                .Map(dest => dest.DetectedSpecies, src => src.DetectedSpecies);
        }
    }
}