using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.RescueMission;

namespace SnakeAid.Core.Mappings
{
    public class RescueMissionMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Map RescueMission → DetailRescueMissionResponse
            config.NewConfig<RescueMission, DetailRescueMissionResponse>()
                .Map(dest => dest.User, src => src.Incident.User);

            // Map SnakebiteIncident → BriefIncidentResponse
            config.NewConfig<SnakebiteIncident, BriefIncidentResponse>()
                .Map(dest => dest.Media, src => src.Media)
                .Map(dest => dest.IdentifiedSnake, src => src.IdentifiedSnakeSpecies);

            // Map RescueMission → CreateRescueMissionResponse
            config.NewConfig<RescueMission, CreateRescueMissionResponse>();
        }
    }
}
