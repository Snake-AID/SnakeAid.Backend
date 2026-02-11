using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakeCatchingRequest;

namespace SnakeAid.Core.Mappings
{
    public class CatchingRequestDetailMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            TypeAdapterConfig<CatchingRequestDetail, CatchingRequestDetailResponse>
                .NewConfig()
                .Map(dest => dest.SnakeSpeciesName, src => src.SnakeSpecies != null ? src.SnakeSpecies.CommonName : null)
                .Map(dest => dest.SnakeSpeciesScientificName, src => src.SnakeSpecies != null ? src.SnakeSpecies.ScientificName : null);
        }
    }
}
