using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.FirstAidGuideline;
using SnakeAid.Core.Responses.FirstAidGuideline;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System.Linq.Expressions;

namespace SnakeAid.Service.Implements
{
    public class FirstAidGuidelineService : IFirstAidGuidelineService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<FirstAidGuidelineService> _logger;

        public FirstAidGuidelineService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<FirstAidGuidelineService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<FirstAidGuidelineResponse> CreateFirstAidGuidelineAsync(CreateFirstAidGuidelineRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var guideline = new FirstAidGuideline
                {
                    Name = request.Name,
                    Content = request.Content,
                    Type = request.Type,
                    Summary = request.Summary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<FirstAidGuideline>().InsertAsync(guideline);
                await _unitOfWork.CommitAsync();

                return guideline.Adapt<FirstAidGuidelineResponse>();
            });
        }

        public async Task<FirstAidGuidelineResponse> GetFirstAidGuidelineByIdAsync(int id)
        {
            var guideline = await _unitOfWork.GetRepository<FirstAidGuideline>()
                .GetByIdAsync(id);

            if (guideline == null)
            {
                throw new NotFoundException($"First aid guideline with ID {id} not found.");
            }

            return guideline.Adapt<FirstAidGuidelineResponse>();
        }

        public async Task<PagedData<FirstAidGuidelineResponse>> FilterFirstAidGuidelinesAsync(GetFirstAidGuidelineRequest request)
        {
            Expression<Func<FirstAidGuideline, bool>>? predicate = null;

            // Build predicate
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Type.HasValue)
            {
                predicate = g => g.Name.Contains(request.Name) && g.Type == request.Type.Value;
            }
            else if (!string.IsNullOrWhiteSpace(request.Name))
            {
                predicate = g => g.Name.Contains(request.Name);
            }
            else if (request.Type.HasValue)
            {
                predicate = g => g.Type == request.Type.Value;
            }

            // Get paginated data
            var pagedData = await _unitOfWork.GetRepository<FirstAidGuideline>()
                .GetPagingListAsync(
                    predicate: predicate,
                    orderBy: o => o.OrderBy(g => g.Name),
                    page: request.PageNumber,
                    size: request.PageSize
                );

            return new PagedData<FirstAidGuidelineResponse>
            {
                Items = pagedData.Items.Adapt<List<FirstAidGuidelineResponse>>(),
                Meta = pagedData.Meta
            };
        }

        public async Task<FirstAidGuidelineResponse> UpdateFirstAidGuidelineAsync(int id, UpdateFirstAidGuidelineRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var guideline = await _unitOfWork.GetRepository<FirstAidGuideline>()
                    .GetByIdAsync(id);

                if (guideline == null)
                {
                    throw new NotFoundException($"First aid guideline with ID {id} not found.");
                }

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    guideline.Name = request.Name;
                }

                if (request.Content != null)
                {
                    guideline.Content = request.Content;
                }

                if (request.Type.HasValue)
                {
                    guideline.Type = request.Type.Value;
                }

                if (request.Summary != null)
                {
                    guideline.Summary = request.Summary;
                }

                guideline.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<FirstAidGuideline>().Update(guideline);
                await _unitOfWork.CommitAsync();

                return guideline.Adapt<FirstAidGuidelineResponse>();
            });
        }

        public async Task DeleteFirstAidGuidelineAsync(int id)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var guideline = await _unitOfWork.GetRepository<FirstAidGuideline>()
                    .GetByIdAsync(id);

                if (guideline == null)
                {
                    throw new NotFoundException($"First aid guideline with ID {id} not found.");
                }

                _unitOfWork.GetRepository<FirstAidGuideline>().Delete(guideline);
                await _unitOfWork.CommitAsync();

                return true; // Just to satisfy the transaction return type
            });
        }

        public async Task<List<FirstAidGuidelineResponse>> GetAllFirstAidGuidelineAsync()
        {
            var guidelines = await _unitOfWork.GetRepository<FirstAidGuideline>()
                .GetListAsync(orderBy: o => o.OrderBy(g => g.Name));

            return guidelines.Adapt<List<FirstAidGuidelineResponse>>();
        }

        public async Task<List<FirstAidGuidelineResponse>> GetFirstAidGuidelinesBySnakeSpeciesIdAsync(int snakeSpeciesId)
        {
            // Check if snake species exists
            var snakeSpecies = await _unitOfWork.GetRepository<SnakeSpecies>()
                .FirstOrDefaultAsync(
                    predicate: s => s.Id == snakeSpeciesId,
                    include: q => q.Include(s => s.SpeciesVenoms)
                                   .ThenInclude(sv => sv.VenomType)
                                   .ThenInclude(vt => vt.FirstAidGuideline)
                );

            if (snakeSpecies == null)
            {
                throw new NotFoundException($"Snake species with ID {snakeSpeciesId} not found.");
            }

            // Get all first aid guidelines from venom types
            var guidelines = snakeSpecies.SpeciesVenoms
                .Where(sv => sv.VenomType != null && sv.VenomType.FirstAidGuideline != null)
                .Select(sv => sv.VenomType.FirstAidGuideline)
                .Distinct()
                .ToList();

            return guidelines.Adapt<List<FirstAidGuidelineResponse>>();
        }
    }
}
