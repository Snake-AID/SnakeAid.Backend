using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SymptomConfig;
using SnakeAid.Core.Responses.SymptomConfig;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System.Text.Json;
using System.Linq.Expressions;

namespace SnakeAid.Service.Implements
{
    public class SymptomConfigService : ISymptomConfigService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SymptomConfigService> _logger;

        public SymptomConfigService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SymptomConfigService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SymptomConfigResponse> CreateSymptomConfigAsync(CreateSymptomConfigRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Validate VenomTypeId if provided
                if (request.VenomTypeId.HasValue)
                {
                    var venomTypeExists = await _unitOfWork.GetRepository<VenomType>()
                        .FirstOrDefaultAsync(predicate: vt => vt.Id == request.VenomTypeId.Value);

                    if (venomTypeExists == null)
                    {
                        throw new NotFoundException($"VenomType with ID {request.VenomTypeId.Value} not found.");
                    }
                }

                var symptomConfig = new SymptomConfig
                {
                    GroupName = request.GroupName,
                    AttributeKey = request.AttributeKey,
                    AttributeLabel = request.AttributeLabel,
                    DisplayOrder = request.DisplayOrder,
                    Name = request.Name,
                    Description = request.Description,
                    IsCritical = request.IsCritical,
                    AlertMessage = request.AlertMessage,
                    IsActive = request.IsActive,
                    Category = request.Category,
                    TimeScoresJson = request.TimeScoreList != null && request.TimeScoreList.Any()
                        ? JsonSerializer.Serialize(request.TimeScoreList)
                        : null,
                    VenomTypeId = request.VenomTypeId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.GetRepository<SymptomConfig>().InsertAsync(symptomConfig);
                await _unitOfWork.CommitAsync();

                // Load VenomType if exists
                if (symptomConfig.VenomTypeId.HasValue)
                {
                    symptomConfig = await _unitOfWork.GetRepository<SymptomConfig>()
                        .FirstOrDefaultAsync(
                            predicate: sc => sc.Id == symptomConfig.Id,
                            include: q => q.Include(sc => sc.VenomType),
                            asNoTracking: false
                        );
                }

                return symptomConfig.Adapt<SymptomConfigResponse>();
            });
        }

        public async Task<SymptomConfigResponse> GetSymptomConfigByIdAsync(int id)
        {
            var symptomConfig = await _unitOfWork.GetRepository<SymptomConfig>()
                .FirstOrDefaultAsync(
                    predicate: sc => sc.Id == id,
                    include: q => q.Include(sc => sc.VenomType)
                );

            if (symptomConfig == null)
            {
                throw new NotFoundException($"Symptom configuration with ID {id} not found.");
            }

            return symptomConfig.Adapt<SymptomConfigResponse>();
        }

        public async Task<PagedData<SymptomConfigResponse>> FilterSymptomConfigsAsync(GetSymptomConfigRequest request)
        {
            Expression<Func<SymptomConfig, bool>>? predicate = null;

            // Build combined predicate
            predicate = sc =>
                (string.IsNullOrWhiteSpace(request.GroupName) || sc.GroupName.Contains(request.GroupName)) &&
                (string.IsNullOrWhiteSpace(request.AttributeKey) || sc.AttributeKey.Contains(request.AttributeKey)) &&
                (string.IsNullOrWhiteSpace(request.Name) || sc.Name.Contains(request.Name)) &&
                (!request.Category.HasValue || sc.Category == request.Category.Value) &&
                (!request.IsActive.HasValue || sc.IsActive == request.IsActive.Value) &&
                (!request.IsCritical.HasValue || sc.IsCritical == request.IsCritical.Value) &&
                (!request.VenomTypeId.HasValue || sc.VenomTypeId == request.VenomTypeId.Value);

            // Get paginated data
            var pagedData = await _unitOfWork.GetRepository<SymptomConfig>()
                .GetPagingListAsync(
                    predicate: predicate,
                    orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.AttributeKey).ThenBy(sc => sc.Name),
                    include: q => q.Include(sc => sc.VenomType),
                    page: request.PageNumber,
                    size: request.PageSize
                );

            var responseItems = pagedData.Items.Adapt<List<SymptomConfigResponse>>();
            return new PagedData<SymptomConfigResponse>
            {
                Items = responseItems,
                Meta = pagedData.Meta
            };
        }

        public async Task<SymptomConfigResponse> UpdateSymptomConfigAsync(int id, UpdateSymptomConfigRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var symptomConfig = await _unitOfWork.GetRepository<SymptomConfig>()
                    .FirstOrDefaultAsync(
                        predicate: sc => sc.Id == id,
                        include: q => q.Include(sc => sc.VenomType),
                        asNoTracking: false
                    );

                if (symptomConfig == null)
                {
                    throw new NotFoundException($"Symptom configuration with ID {id} not found.");
                }

                // Validate VenomTypeId if provided
                if (request.VenomTypeId.HasValue)
                {
                    var venomTypeExists = await _unitOfWork.GetRepository<VenomType>()
                        .FirstOrDefaultAsync(predicate: vt => vt.Id == request.VenomTypeId.Value);

                    if (venomTypeExists == null)
                    {
                        throw new NotFoundException($"VenomType with ID {request.VenomTypeId.Value} not found.");
                    }
                }

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.GroupName))
                {
                    symptomConfig.GroupName = request.GroupName;
                }

                if (!string.IsNullOrWhiteSpace(request.AttributeKey))
                {
                    symptomConfig.AttributeKey = request.AttributeKey;
                }

                if (!string.IsNullOrWhiteSpace(request.AttributeLabel))
                {
                    symptomConfig.AttributeLabel = request.AttributeLabel;
                }


                if (request.DisplayOrder.HasValue)
                {
                    symptomConfig.DisplayOrder = request.DisplayOrder.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    symptomConfig.Name = request.Name;
                }

                if (request.Description != null)
                {
                    symptomConfig.Description = request.Description;
                }

                if (request.IsCritical.HasValue)
                {
                    symptomConfig.IsCritical = request.IsCritical.Value;
                }

                if (request.AlertMessage != null)
                {
                    symptomConfig.AlertMessage = request.AlertMessage;
                }

                if (request.IsActive.HasValue)
                {
                    symptomConfig.IsActive = request.IsActive.Value;
                }

                if (request.Category.HasValue)
                {
                    symptomConfig.Category = request.Category.Value;
                }

                if (request.TimeScoreList != null)
                {
                    symptomConfig.TimeScoresJson = request.TimeScoreList.Any()
                        ? JsonSerializer.Serialize(request.TimeScoreList)
                        : null;
                }

                if (request.VenomTypeId != null)
                {
                    symptomConfig.VenomTypeId = request.VenomTypeId;
                }

                symptomConfig.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.GetRepository<SymptomConfig>().Update(symptomConfig);
                await _unitOfWork.CommitAsync();

                // Reload to get updated VenomType
                if (symptomConfig.VenomTypeId.HasValue)
                {
                    symptomConfig = await _unitOfWork.GetRepository<SymptomConfig>()
                        .FirstOrDefaultAsync(
                            predicate: sc => sc.Id == id,
                            include: q => q.Include(sc => sc.VenomType),
                            asNoTracking: false
                        );
                }

                return symptomConfig.Adapt<SymptomConfigResponse>();
            });
        }

        public async Task DeleteSymptomConfigAsync(int id)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var symptomConfig = await _unitOfWork.GetRepository<SymptomConfig>()
                    .GetByIdAsync(id);

                if (symptomConfig == null)
                {
                    throw new NotFoundException($"Symptom configuration with ID {id} not found.");
                }

                _unitOfWork.GetRepository<SymptomConfig>().Delete(symptomConfig);
                await _unitOfWork.CommitAsync();

                return true; // Just to satisfy the transaction return type
            });
        }

        public async Task<Dictionary<string, List<SymptomConfigResponse>>> GetSymptomConfigsGroupedByKeyAsync()
        {
            var symptomConfigs = await _unitOfWork.GetRepository<SymptomConfig>()
                .GetListAsync(
                    predicate: sc => sc.IsActive,
                    orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name),
                    include: q => q.Include(sc => sc.VenomType)
                );

            return symptomConfigs
                .GroupBy(sc => sc.AttributeKey)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList().Adapt<List<SymptomConfigResponse>>()
                );
        }

        public async Task<List<SymptomConfigResponse>> GetAllSymptomConfigAsync()
        {
            var symptomConfigs = await _unitOfWork.GetRepository<SymptomConfig>()
                .GetListAsync(
                    orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name),
                    include: q => q.Include(sc => sc.VenomType)
                );

            return symptomConfigs.Adapt<List<SymptomConfigResponse>>();
        }
    }
}
