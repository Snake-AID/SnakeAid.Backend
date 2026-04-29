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
                .Map(dest => dest.MissionMedia, src => src.Media)
                .Map(dest => dest.User, src => src.Incident.User)
                .Map(dest => dest.HospitalInfo, src => src.Hospital);

            // Map SnakebiteIncident → BriefIncidentResponse
            config.NewConfig<SnakebiteIncident, BriefIncidentResponse>()
                .Map(dest => dest.Media, src => src.Media)
                .Map(dest => dest.IdentifiedSnake, src => src.IdentifiedSnakeSpecies);

            // Map RescueMission → CreateRescueMissionResponse
            config.NewConfig<RescueMission, CreateRescueMissionResponse>();

            config.NewConfig<TreatmentFacility, HospitalTransferResponse>()
                .Map(dest => dest.HospitalId, src => src.Id)
                .Map(dest => dest.HospitalName, src => src.Name)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.ContactNumber, src => src.ContactNumber);
        }
    }
}
