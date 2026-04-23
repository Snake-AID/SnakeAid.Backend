using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.CommunityReport;
using SnakeAid.Core.Responses.CommunityReport;
using SnakeAid.Core.Responses.SnakeSpecies;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class CommunityReportService : ICommunityReportService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public CommunityReportService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CommunityReportResponse> CreateCommunityReportAsync(CreateCommunityReportRequest request, Guid userId)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            var userExists = await _unitOfWork.GetRepository<Account>()
                .FirstOrDefaultAsync(predicate: a => a.Id == userId);

            if (userExists == null)
            {
                throw new NotFoundException("User not found.");
            }

            SnakeSpecies? snakeSpecies = null;
            if (request.SnakeSpeciesId.HasValue)
            {
                snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .FirstOrDefaultAsync(predicate: s => s.Id == request.SnakeSpeciesId.Value);

                if (snakeSpecies == null)
                {
                    throw new NotFoundException($"Snake species with id '{request.SnakeSpeciesId.Value}' not found.");
                }
            }

            var report = new CommunityReport
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LocationCoordinates = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                Notes = request.Notes,
                SnakeSpeciesId = request.SnakeSpeciesId

            };

            await _unitOfWork.GetRepository<CommunityReport>().InsertAsync(report);
            await _unitOfWork.CommitAsync();

            report.User = userExists;
            report.SnakeSpecies = snakeSpecies;
            return ToResponse(report);
        }

        public async Task<CommunityReportResponse> GetCommunityReportByIdAsync(Guid id, Guid currentUserId, string currentUserRole)
        {
            var report = await _unitOfWork.GetRepository<CommunityReport>()
                .FirstOrDefaultAsync(
                    predicate: r => r.Id == id,
                    include: query => query.Include(r => r.User)
                                           .Include(r => r.SnakeSpecies));

            if (report == null)
            {
                throw new NotFoundException($"Community report with id '{id}' not found.");
            }

            EnsureCanAccess(report, currentUserId, currentUserRole);
            return ToResponse(report);
        }

        public async Task<List<CommunityReportResponse>> GetCommunityReportsAsync(Guid currentUserId, string currentUserRole)
        {
            var isAdmin = IsAdmin(currentUserRole);

            var reports = await _unitOfWork.GetRepository<CommunityReport>()
                .GetListAsync(
                    predicate: isAdmin ? null : r => r.UserId == currentUserId,
                    orderBy: query => query.OrderByDescending(r => r.CreatedAt),
                    include: query => query.Include(r => r.User)
                                           .Include(r => r.SnakeSpecies));

            return reports.Select(ToResponse).ToList();
        }
        public async Task<List<CommunityReportResponse>> GetAllCommunityReportsAsync()
        {
            var reports = await _unitOfWork.GetRepository<CommunityReport>()
                .GetListAsync(
                    predicate: null,
                    orderBy: query => query.OrderByDescending(r => r.CreatedAt),
                    include: query => query.Include(r => r.User)
                                           .Include(r => r.SnakeSpecies));

            return reports.Select(ToResponse).ToList();
        }


        public async Task<CommunityReportResponse> UpdateCommunityReportAsync(Guid id, UpdateCommunityReportRequest request, Guid currentUserId, string currentUserRole)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            var report = await _unitOfWork.GetRepository<CommunityReport>()
                .FirstOrDefaultAsync(
                    predicate: r => r.Id == id,
                    include: query => query.Include(r => r.User)
                                           .Include(r => r.SnakeSpecies),
                    asNoTracking: false);

            if (report == null)
            {
                throw new NotFoundException($"Community report with id '{id}' not found.");
            }

            EnsureCanAccess(report, currentUserId, currentUserRole);

            if (request.Longitude.HasValue ^ request.Latitude.HasValue)
            {
                throw new BadRequestException("Both longitude and latitude must be provided together.");
            }

            if (request.Longitude.HasValue && request.Latitude.HasValue)
            {
                report.LocationCoordinates = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
            }

            if (request.Notes != null)
            {
                report.Notes = request.Notes;
            }

            if (request.SnakeSpeciesId.HasValue)
            {
                var snakeSpeciesExists = await _unitOfWork.GetRepository<SnakeSpecies>()
                    .FirstOrDefaultAsync(predicate: s => s.Id == request.SnakeSpeciesId.Value);
                if (snakeSpeciesExists == null)
                {
                    throw new NotFoundException($"Snake species with id '{request.SnakeSpeciesId.Value}' not found.");
                }
                report.SnakeSpeciesId = request.SnakeSpeciesId;
                report.SnakeSpecies = snakeSpeciesExists;
            }

            _unitOfWork.GetRepository<CommunityReport>().Update(report);
            await _unitOfWork.CommitAsync();

            return ToResponse(report);
        }

        public async Task DeleteCommunityReportAsync(Guid id, Guid currentUserId, string currentUserRole)
        {
            var report = await _unitOfWork.GetRepository<CommunityReport>()
                .FirstOrDefaultAsync(
                    predicate: r => r.Id == id,
                    asNoTracking: false);

            if (report == null)
            {
                throw new NotFoundException($"Community report with id '{id}' not found.");
            }

            EnsureCanAccess(report, currentUserId, currentUserRole);

            _unitOfWork.GetRepository<CommunityReport>().Delete(report);
            await _unitOfWork.CommitAsync();
        }

        private static bool IsAdmin(string currentUserRole)
        {
            return string.Equals(currentUserRole, AccountRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureCanAccess(CommunityReport report, Guid currentUserId, string currentUserRole)
        {
            if (IsAdmin(currentUserRole))
            {
                return;
            }

            if (report.UserId != currentUserId)
            {
                throw new ForbiddenException("You are not allowed to access this community report.");
            }
        }

        private static CommunityReportResponse ToResponse(CommunityReport report)
        {
            return new CommunityReportResponse
            {
                Id = report.Id,
                UserId = report.UserId,
                ReporterName = report.User?.FullName,
                Longitude = report.LocationCoordinates?.X ?? 0,
                Latitude = report.LocationCoordinates?.Y ?? 0,
                Notes = report.Notes,
                SnakeSpecies = report.SnakeSpecies == null
                    ? null
                    : new SnakeSpeciesResponse
                    {
                        Id = report.SnakeSpecies.Id,
                        ScientificName = report.SnakeSpecies.ScientificName,
                        Slug = report.SnakeSpecies.Slug,
                        CommonName = report.SnakeSpecies.CommonName,
                        ImageUrl = report.SnakeSpecies.ImageUrl,
                        Description = report.SnakeSpecies.Description,
                        IdentificationSummary = report.SnakeSpecies.IdentificationSummary,
                        PrimaryVenomType = report.SnakeSpecies.PrimaryVenomType,
                        RiskLevel = report.SnakeSpecies.RiskLevel,
                        IsVenomous = report.SnakeSpecies.IsVenomous,
                        IsActive = report.SnakeSpecies.IsActive,
                    },
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }
    }
}
