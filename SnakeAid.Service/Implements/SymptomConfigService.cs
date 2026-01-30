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

        public async Task<ApiResponse<SymptomConfigResponse>> CreateSymptomConfigAsync(CreateSymptomConfigRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request data cannot be null.");
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

                    var symptomConfig = new Core.Domains.SymptomConfig
                    {
                        AttributeKey = request.AttributeKey,
                        AttributeLabel = request.AttributeLabel,
                        UIHint = request.UIHint,
                        DisplayOrder = request.DisplayOrder,
                        Name = request.Name,
                        Description = request.Description,
                        IsActive = request.IsActive,
                        Category = request.Category,
                        TimeScoresJson = request.TimeScoreList != null && request.TimeScoreList.Any()
                            ? JsonSerializer.Serialize(request.TimeScoreList)
                            : null,
                        VenomTypeId = request.VenomTypeId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>().InsertAsync(symptomConfig);
                    await _unitOfWork.CommitAsync();

                    // Load VenomType if exists
                    if (symptomConfig.VenomTypeId.HasValue)
                    {
                        symptomConfig = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                            .FirstOrDefaultAsync(
                                predicate: sc => sc.Id == symptomConfig.Id,
                                include: q => q.Include(sc => sc.VenomType),
                                asNoTracking: false
                            );
                    }

                    var response = MapToResponse(symptomConfig);
                    return ApiResponseBuilder.BuildSuccessResponse(response, "Symptom configuration created successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating symptom configuration");
                return ApiResponseBuilder.BuildFailureResponse<SymptomConfigResponse>(ex.Message);
            }
        }

        public async Task<ApiResponse<SymptomConfigResponse>> GetSymptomConfigByIdAsync(int id)
        {
            try
            {
                var symptomConfig = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                    .FirstOrDefaultAsync(
                        predicate: sc => sc.Id == id,
                        include: q => q.Include(sc => sc.VenomType)
                    );

                if (symptomConfig == null)
                {
                    throw new NotFoundException($"Symptom configuration with ID {id} not found.");
                }

                var response = MapToResponse(symptomConfig);
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symptom configuration by ID {Id}", id);
                return ApiResponseBuilder.BuildFailureResponse<SymptomConfigResponse>(ex.Message);
            }
        }

        public async Task<ApiResponse<PagedData<SymptomConfigResponse>>> FilterSymptomConfigsAsync(GetSymptomConfigRequest request)
        {
            try
            {
                var query = _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                    .GetQueryable()
                    .Include(sc => sc.VenomType);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.AttributeKey))
                {
                    query = query.Where(sc => sc.AttributeKey.Contains(request.AttributeKey));
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    query = query.Where(sc => sc.Name.Contains(request.Name));
                }

                if (request.UIHint.HasValue)
                {
                    query = query.Where(sc => sc.UIHint == request.UIHint.Value);
                }

                if (request.Category.HasValue)
                {
                    query = query.Where(sc => sc.Category == request.Category.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(sc => sc.IsActive == request.IsActive.Value);
                }

                if (request.VenomTypeId.HasValue)
                {
                    query = query.Where(sc => sc.VenomTypeId == request.VenomTypeId.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply ordering
                query = query.OrderBy(sc => sc.DisplayOrder)
                            .ThenBy(sc => sc.AttributeKey)
                            .ThenBy(sc => sc.Name);

                // Apply pagination
                var items = await query
                    .Skip((request.Page - 1) * request.Size)
                    .Take(request.Size)
                    .ToListAsync();

                var responseItems = items.Select(MapToResponse).ToList();

                var pagedData = new PagedData<SymptomConfigResponse>
                {
                    Items = responseItems,
                    TotalCount = totalCount,
                    Page = request.Page,
                    Size = request.Size,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.Size)
                };

                return ApiResponseBuilder.BuildSuccessResponse(pagedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symptom configurations");
                return ApiResponseBuilder.BuildFailureResponse<PagedData<SymptomConfigResponse>>(ex.Message);
            }
        }

        public async Task<ApiResponse<SymptomConfigResponse>> UpdateSymptomConfigAsync(int id, UpdateSymptomConfigRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request data cannot be null.");
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var symptomConfig = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
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
                    if (!string.IsNullOrWhiteSpace(request.AttributeKey))
                    {
                        symptomConfig.AttributeKey = request.AttributeKey;
                    }

                    if (!string.IsNullOrWhiteSpace(request.AttributeLabel))
                    {
                        symptomConfig.AttributeLabel = request.AttributeLabel;
                    }

                    if (request.UIHint.HasValue)
                    {
                        symptomConfig.UIHint = request.UIHint.Value;
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

                    _unitOfWork.GetRepository<Core.Domains.SymptomConfig>().Update(symptomConfig);
                    await _unitOfWork.CommitAsync();

                    // Reload to get updated VenomType
                    if (symptomConfig.VenomTypeId.HasValue)
                    {
                        symptomConfig = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                            .FirstOrDefaultAsync(
                                predicate: sc => sc.Id == id,
                                include: q => q.Include(sc => sc.VenomType),
                                asNoTracking: false
                            );
                    }

                    var response = MapToResponse(symptomConfig);
                    return ApiResponseBuilder.BuildSuccessResponse(response, "Symptom configuration updated successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating symptom configuration with ID {Id}", id);
                return ApiResponseBuilder.BuildFailureResponse<SymptomConfigResponse>(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteSymptomConfigAsync(int id)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var symptomConfig = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                        .GetByIdAsync(id);

                    if (symptomConfig == null)
                    {
                        throw new NotFoundException($"Symptom configuration with ID {id} not found.");
                    }

                    _unitOfWork.GetRepository<Core.Domains.SymptomConfig>().Delete(symptomConfig);
                    await _unitOfWork.CommitAsync();

                    return ApiResponseBuilder.BuildSuccessResponse(true, "Symptom configuration deleted successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting symptom configuration with ID {Id}", id);
                return ApiResponseBuilder.BuildFailureResponse<bool>(ex.Message);
            }
        }

        public async Task<ApiResponse<Dictionary<string, List<SymptomConfigResponse>>>> GetSymptomConfigsGroupedByKeyAsync()
        {
            try
            {
                var symptomConfigs = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                    .GetListAsync(
                        predicate: sc => sc.IsActive,
                        orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name),
                        include: q => q.Include(sc => sc.VenomType)
                    );

                var grouped = symptomConfigs
                    .GroupBy(sc => sc.AttributeKey)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(MapToResponse).ToList()
                    );

                return ApiResponseBuilder.BuildSuccessResponse(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symptom configurations grouped by key");
                return ApiResponseBuilder.BuildFailureResponse<Dictionary<string, List<SymptomConfigResponse>>>(ex.Message);
            }
        }

        public async Task<ApiResponse<List<SymptomConfigResponse>>> GetAllSymptomConfigAsync()
        {
            try
            {
                var symptomConfigs = await _unitOfWork.GetRepository<Core.Domains.SymptomConfig>()
                    .GetListAsync(
                        orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name),
                        include: q => q.Include(sc => sc.VenomType)
                    );

                var response = symptomConfigs.Select(MapToResponse).ToList();
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all symptom configurations");
                return ApiResponseBuilder.BuildFailureResponse<List<SymptomConfigResponse>>(ex.Message);
            }
        }

        private SymptomConfigResponse MapToResponse(Core.Domains.SymptomConfig symptomConfig)
        {
            return new SymptomConfigResponse
            {
                Id = symptomConfig.Id,
                AttributeKey = symptomConfig.AttributeKey,
                AttributeLabel = symptomConfig.AttributeLabel,
                UIHint = symptomConfig.UIHint,
                UIHintDisplay = symptomConfig.UIHint.ToString(),
                DisplayOrder = symptomConfig.DisplayOrder,
                Name = symptomConfig.Name,
                Description = symptomConfig.Description,
                IsActive = symptomConfig.IsActive,
                Category = symptomConfig.Category,
                CategoryDisplay = symptomConfig.Category.ToString(),
                TimeScoreList = symptomConfig.TimeScoreList,
                VenomTypeId = symptomConfig.VenomTypeId,
                VenomType = symptomConfig.VenomType != null
                    ? new VenomTypeInfo
                    {
                        Id = symptomConfig.VenomType.Id,
                        Name = symptomConfig.VenomType.Name
                    }
                    : null,
                CreatedAt = symptomConfig.CreatedAt,
                UpdatedAt = symptomConfig.UpdatedAt
            };
        }
    }
}
