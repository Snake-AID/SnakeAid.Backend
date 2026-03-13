using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Core.Responses.FirstAidGuideline;
using SnakeAid.Core.Responses.SymptomConfig;
using SnakeAid.Core.Responses.Media;

namespace SnakeAid.Core.Mappings
{
    public class DomainMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Global config for NetTopologySuite Point - use shallow copy (reference)
            config.NewConfig<NetTopologySuite.Geometries.Point, NetTopologySuite.Geometries.Point>()
                .MapWith(src => src);

            // FirstAidGuideline mappings
            config.NewConfig<FirstAidGuideline, FirstAidGuidelineResponse>();

            // SymptomConfig mappings
            config.NewConfig<SymptomConfig, SymptomConfigResponse>()
                .Map(dest => dest.CategoryDisplay, src => src.Category.ToString())
                .Map(dest => dest.VenomType, src => src.VenomType != null
                    ? new VenomTypeInfo
                    {
                        Id = src.VenomType.Id,
                        Name = src.VenomType.Name
                    }
                    : null);

            // VenomType to VenomTypeInfo mapping
            config.NewConfig<VenomType, VenomTypeInfo>();
        }
    }
}