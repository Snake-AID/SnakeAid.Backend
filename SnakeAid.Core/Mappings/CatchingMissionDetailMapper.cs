using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakeCatchingMission;

namespace SnakeAid.Core.Mappings
{
    public class CatchingMissionDetailMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            TypeAdapterConfig<CatchingMissionDetail, CatchingMissionDetailResponse>
                .NewConfig()
                .Map(dest => dest.SnakeSpeciesName, src => src.SnakeSpecies != null ? src.SnakeSpecies.CommonName : null);
        }
    }
}
