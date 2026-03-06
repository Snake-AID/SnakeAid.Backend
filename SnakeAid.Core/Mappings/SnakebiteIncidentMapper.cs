using Mapster;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses;
using SnakeAid.Core.Responses.Auth;
using SnakeAid.Core.Responses.MemberProfile;
using SnakeAid.Core.Responses.RescuerProfile;
using SnakeAid.Core.Responses.SnakebiteIncident;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Mappings
{
    public class SnakebiteIncidentMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Map Point → GeoPointResponse
            config.NewConfig<Point, GeoPointResponse>()
                .Map(dest => dest.Latitude, src => src.Y)
                .Map(dest => dest.Longitude, src => src.X);

            // Map Incident → Response
            config.NewConfig<SnakebiteIncident, CreateIncidentResponse>()
                .Map(dest => dest.LocationCoordinates, src => src.LocationCoordinates);

            config.NewConfig<SnakebiteIncident, DetailSnakebiteIncidentResponse>()
                // Map ActiveMission: only return active missions (not aborted/cancelled)
                .Map(dest => dest.ActiveMission, src =>
                    src.Missions
                        .Where(m => m.Status != RescueMissionStatus.MissionAborted && m.Status != RescueMissionStatus.Cancelled)
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefault())
                // Count total attempts (all missions regardless of status)
                .Map(dest => dest.TotalRescueAttempts, src => src.Missions.Count)
                // Count failed attempts (only aborted missions)
                .Map(dest => dest.FailedAttemptsCount, src =>
                    src.Missions.Count(m => m.Status == RescueMissionStatus.MissionAborted))
                // Map Media with AI detection results
                .Map(dest => dest.Media, src => src.Media);
        }
    }
}
