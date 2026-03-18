using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.TreatmentFacility;
using SnakeAid.Core.Responses.TreatmentFacility;

namespace SnakeAid.Service.Interfaces
{
    public interface ITreatmentFacilityService
    {
        public Task<IEnumerable<TreatmentFacilityResponse>> GetNearestActiveTreatmentFacilityAsync(double latitude, double longitude);

        public Task<IEnumerable<TreatmentFacilityResponse>> GetAllTreatmentFacilitiesAsync();

        public Task<TreatmentFacilityResponse> CreateTreatmentFacilityAsync(CreateTreatmentFacilityRequest request);

        public Task<TreatmentFacilityResponse> UpdateTreatmentFacilityAsync(int id, UpdateTreatmentFacilityRequest request);

        public Task<bool> DeleteTreatmentFacilityAsync(int id);
    }
}