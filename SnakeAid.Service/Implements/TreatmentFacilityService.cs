using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.TreatmentFacility;
using SnakeAid.Core.Responses.TreatmentFacility;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using NetTopologySuite.Geometries;

namespace SnakeAid.Service.Implements
{
    public class TreatmentFacilityService : ITreatmentFacilityService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<TreatmentFacilityService> _logger;
        private readonly IConfiguration _configuration;

        public TreatmentFacilityService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<TreatmentFacilityService> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }


        // Default search distance in meters (30 km)
        private const decimal DEFAUT_SEARCH_DISTANCE = 30000; // 30 km

        public async Task<IEnumerable<TreatmentFacilityResponse>> GetNearestActiveTreatmentFacilityAsync(double latitude, double longitude)
        {
            // Create a Point for the user's location (SRID 4326 for WGS84)
            var userPoint = new Point(longitude, latitude) { SRID = 4326 };

            const double MaxDistanceMeters = 30000; // 30km in meters

            // Query with spatial ordering and optional distance filter
            var nearbyHospitals = await _unitOfWork
                .GetRepository<TreatmentFacility>()
                .GetListAsync<TreatmentFacilityResponse>(
                    predicate: h => h.IsActive &&
                        EF.Functions.IsWithinDistance(h.Location, userPoint, MaxDistanceMeters, true),
                    include: q => q.Include(h => h.AntivenomStocks),
                    orderBy: q => q.OrderBy(h => h.Location.Distance(userPoint)),
                    selector: h => new TreatmentFacilityResponse
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Address = h.Address,
                        ContactNumber = h.ContactNumber,
                        Latitude = h.Location.Y,
                        Longitude = h.Location.X,
                        DistanceKm = EF.Functions.Distance(h.Location, userPoint, true) / 1000,
                        AntivenomIds = h.AntivenomStocks.Select(x => x.Id).ToList()
                    }
                );

            return nearbyHospitals;
        }

        public async Task<TreatmentFacilityResponse> GetTreatmentFacilityByIdAsync(int id)
        {
            var facility = await _unitOfWork.GetRepository<TreatmentFacility>()
                .FirstOrDefaultAsync(
                    predicate: x => x.Id == id,
                    include: q => q.Include(x => x.AntivenomStocks));

            if (facility == null)
            {
                throw new NotFoundException("Treatment facility not found.");
            }

            return MapToResponse(facility);
        }


        public async Task<IEnumerable<TreatmentFacilityResponse>> GetAllTreatmentFacilitiesAsync()
        {
            var nearbyHospitals = await _unitOfWork
                .GetRepository<TreatmentFacility>()
                .GetListAsync<TreatmentFacilityResponse>(
                    include: q => q.Include(h => h.AntivenomStocks),
                    orderBy: q => q.OrderBy(h => h.Id),
                    selector: h => new TreatmentFacilityResponse
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Address = h.Address,
                        ContactNumber = h.ContactNumber,
                        Latitude = h.Location.Y,
                        Longitude = h.Location.X,
                        DistanceKm = 0,
                        IsActive = h.IsActive,
                        AntivenomIds = h.AntivenomStocks.Select(x => x.Id).ToList()
                    }
                );

            return nearbyHospitals;
        }

        public async Task<PagedData<TreatmentFacilityResponse>> FilterTreatmentFacilitiesAsync(GetTreatmentFacilityRequest request)
        {
            var normalizedName = request.Name?.Trim();

            var pagedData = await _unitOfWork
                .GetRepository<TreatmentFacility>()
                .GetPagingListAsync(
                    selector: h => new TreatmentFacilityResponse
                    {
                        Id = h.Id,
                        Name = h.Name,
                        Address = h.Address,
                        ContactNumber = h.ContactNumber,
                        Latitude = h.Location.Y,
                        Longitude = h.Location.X,
                        DistanceKm = 0,
                        IsActive = h.IsActive,
                        AntivenomIds = h.AntivenomStocks.Select(x => x.Id).ToList()
                    },
                    predicate: h =>
                        (string.IsNullOrWhiteSpace(normalizedName) || h.Name.Contains(normalizedName))
                        && (!request.IsActive.HasValue || h.IsActive == request.IsActive.Value)
                        && (!request.AntivenomId.HasValue || h.AntivenomStocks.Any(a => a.Id == request.AntivenomId.Value)),
                    include: q => q.Include(h => h.AntivenomStocks),
                    orderBy: q => q.OrderBy(h => h.Id),
                    page: request.PageNumber,
                    size: request.PageSize);

            return new PagedData<TreatmentFacilityResponse>
            {
                Items = pagedData.Items,
                Meta = pagedData.Meta
            };
        }

        public async Task<TreatmentFacilityResponse> CreateTreatmentFacilityAsync(CreateTreatmentFacilityRequest request)
        {
            try
            {
                if (request.Longitude == 0 || request.Latitude == 0)
                    throw new ArgumentException("Latitude and Longitude must be provided and non-zero.");

                if (request.Longitude < -180 || request.Longitude > 180)
                    throw new ArgumentOutOfRangeException(nameof(request.Longitude), "Longitude must be between -180 and 180.");

                if (request.Latitude < -90 || request.Latitude > 90)
                    throw new ArgumentOutOfRangeException(nameof(request.Latitude), "Latitude must be between -90 and 90.");

                // Create a new TreatmentFacility entity
                var newFacility = new TreatmentFacility
                {
                    Name = request.Name,
                    Address = request.Address,
                    ContactNumber = request.ContactNumber,
                    Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                    IsActive = request.IsActive
                };

                var antivenoms = await ResolveAntivenomsByIdsAsync(request.AntivenomIds);
                foreach (var antivenom in antivenoms)
                {
                    newFacility.AntivenomStocks.Add(antivenom);
                }

                await EnsureTreatmentFacilityIdSequenceAsync();

                var createdFacility = await _unitOfWork.GetRepository<TreatmentFacility>().InsertAsync(newFacility);
                var result = await _unitOfWork.CommitAsync();

                if (result <= 0)
                    throw new InvalidOperationException("Failed to create treatment facility.");

                return MapToResponse(createdFacility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating treatment facility with name {Name}", request.Name);
                throw;
            }
        }

        public async Task<TreatmentFacilityResponse> UpdateTreatmentFacilityAsync(int id, UpdateTreatmentFacilityRequest request)
        {
            try
            {
                // if request lat lng is null or zero, only update other provided fields, do not update location

                var repo = _unitOfWork.GetRepository<TreatmentFacility>();

                var existingFacility = await repo.FirstOrDefaultAsync(
                    predicate: f => f.Id == id,
                    include: q => q.Include(x => x.AntivenomStocks),
                    asNoTracking: false);

                if (existingFacility == null)
                    throw new InvalidOperationException("Treatment facility not found.");

                if (request.Longitude != null && (request.Longitude < -180 || request.Longitude > 180))
                    throw new ArgumentOutOfRangeException(nameof(request.Longitude), "Longitude must be between -180 and 180.");

                if (request.Latitude != null && (request.Latitude < -90 || request.Latitude > 90))
                    throw new ArgumentOutOfRangeException(nameof(request.Latitude), "Latitude must be between -90 and 90.");


                existingFacility.Name = request.Name;
                existingFacility.Address = request.Address;
                if (request.Longitude != null && request.Latitude != null)
                {
                    existingFacility.Location = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
                }
                existingFacility.ContactNumber = request.ContactNumber;
                existingFacility.IsActive = request.IsActive;

                if (request.AntivenomIds != null)
                {
                    var antivenoms = await ResolveAntivenomsByIdsAsync(request.AntivenomIds);
                    existingFacility.AntivenomStocks.Clear();
                    foreach (var antivenom in antivenoms)
                    {
                        existingFacility.AntivenomStocks.Add(antivenom);
                    }
                }

                repo.Update(existingFacility);
                var result = await _unitOfWork.CommitAsync();

                if (result <= 0)
                    throw new InvalidOperationException("Failed to update treatment facility.");

                return MapToResponse(existingFacility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating treatment facility with Id {id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTreatmentFacilityAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<TreatmentFacility>();

                var existingFacility = await repo.FirstOrDefaultAsync(predicate: f => f.Id == id);

                if (existingFacility == null)
                    throw new InvalidOperationException("Treatment facility not found.");

                existingFacility.IsActive = false;

                repo.Update(existingFacility);
                var result = await _unitOfWork.CommitAsync();

                if (result <= 0)
                    throw new InvalidOperationException("Failed to update treatment facility.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating treatment facility with Id {id}", id);
                throw;
            }
        }

        private async Task<List<Antivenom>> ResolveAntivenomsByIdsAsync(List<int>? antivenomIds)
        {
            if (antivenomIds == null || antivenomIds.Count == 0)
            {
                return new List<Antivenom>();
            }

            var normalizedIds = antivenomIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (normalizedIds.Count != antivenomIds.Count)
            {
                throw new BadRequestException("AntivenomIds contains invalid value(s).");
            }

            var antivenoms = await _unitOfWork.GetRepository<Antivenom>()
                .GetListAsync(predicate: x => normalizedIds.Contains(x.Id), asNoTracking: false);

            var missingIds = normalizedIds.Except(antivenoms.Select(x => x.Id)).ToList();
            if (missingIds.Count > 0)
            {
                throw new NotFoundException($"Antivenom not found for ID(s): {string.Join(", ", missingIds)}");
            }

            return antivenoms.ToList();
        }

        private static TreatmentFacilityResponse MapToResponse(TreatmentFacility facility)
        {
            return new TreatmentFacilityResponse
            {
                Id = facility.Id,
                Name = facility.Name,
                Address = facility.Address,
                ContactNumber = facility.ContactNumber,
                Latitude = facility.Location.Y,
                Longitude = facility.Location.X,
                DistanceKm = 0,
                IsActive = facility.IsActive,
                AntivenomIds = facility.AntivenomStocks.Select(x => x.Id).ToList()
            };
        }

        private async Task EnsureTreatmentFacilityIdSequenceAsync()
        {
            const string sql = @"
SELECT setval(
    pg_get_serial_sequence('""SnakeAid"".""TreatmentFacilities""', 'Id'),
    GREATEST((SELECT COALESCE(MAX(""Id""), 1) FROM ""SnakeAid"".""TreatmentFacilities""), 1),
    true
);";

            await _unitOfWork.Context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}