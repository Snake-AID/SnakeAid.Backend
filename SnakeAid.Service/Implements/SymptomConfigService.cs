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
using System.Net;

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

                    var symptomConfig = new SymptomConfig
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

                    var response = symptomConfig.Adapt<SymptomConfigResponse>();
                    return ApiResponseBuilder.BuildSuccessResponse(response, "Symptom configuration created successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating symptom configuration");
                return ApiResponseBuilder.CreateResponse<SymptomConfigResponse>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<SymptomConfigResponse>> GetSymptomConfigByIdAsync(int id)
        {
            try
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

                var response = symptomConfig.Adapt<SymptomConfigResponse>();
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symptom configuration by ID {Id}", id);
                return ApiResponseBuilder.CreateResponse<SymptomConfigResponse>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<PagedData<SymptomConfigResponse>>> FilterSymptomConfigsAsync(GetSymptomConfigRequest request)
        {
            try
            {
                Expression<Func<SymptomConfig, bool>>? predicate = null;

                // Build combined predicate
                predicate = sc => 
                    (string.IsNullOrWhiteSpace(request.AttributeKey) || sc.AttributeKey.Contains(request.AttributeKey)) &&
                    (string.IsNullOrWhiteSpace(request.Name) || sc.Name.Contains(request.Name)) &&
                    (!request.UIHint.HasValue || sc.UIHint == request.UIHint.Value) &&
                    (!request.Category.HasValue || sc.Category == request.Category.Value) &&
                    (!request.IsActive.HasValue || sc.IsActive == request.IsActive.Value) &&
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
                var response = new PagedData<SymptomConfigResponse>
                {
                    Items = responseItems,
                    Meta = pagedData.Meta
                };

                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering symptom configurations");
                return ApiResponseBuilder.CreateResponse<PagedData<SymptomConfigResponse>>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
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

                    var response = symptomConfig.Adapt<SymptomConfigResponse>();
                    return ApiResponseBuilder.BuildSuccessResponse(response, "Symptom configuration updated successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating symptom configuration with ID {Id}", id);
                return ApiResponseBuilder.CreateResponse<SymptomConfigResponse>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<bool>> DeleteSymptomConfigAsync(int id)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var symptomConfig = await _unitOfWork.GetRepository<SymptomConfig>()
                        .GetByIdAsync(id);

                    if (symptomConfig == null)
                    {
                        throw new NotFoundException($"Symptom configuration with ID {id} not found.");
                    }

                    _unitOfWork.GetRepository<SymptomConfig>().Delete(symptomConfig);
                    await _unitOfWork.CommitAsync();

                    return ApiResponseBuilder.BuildSuccessResponse(true, "Symptom configuration deleted successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting symptom configuration with ID {Id}", id);
                return ApiResponseBuilder.CreateResponse<bool>(
                    false, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<Dictionary<string, List<SymptomConfigResponse>>>> GetSymptomConfigsGroupedByKeyAsync()
        {
            try
            {
                var symptomConfigs = await _unitOfWork.GetRepository<SymptomConfig>()
                    .GetListAsync(
                        predicate: sc => sc.IsActive,
                        orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name),
                        include: q => q.Include(sc => sc.VenomType)
                    );

                var grouped = symptomConfigs
                    .GroupBy(sc => sc.AttributeKey)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList().Adapt<List<SymptomConfigResponse>>()
                    );

                return ApiResponseBuilder.BuildSuccessResponse(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting symptom configurations grouped by key");
                return ApiResponseBuilder.CreateResponse<Dictionary<string, List<SymptomConfigResponse>>>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<List<SymptomConfigResponse>>> GetAllSymptomConfigAsync()
        {
            try
            {
                var symptomConfigs = await _unitOfWork.GetRepository<SymptomConfig>()
                    .GetListAsync(
                        orderBy: o => o.OrderBy(sc => sc.DisplayOrder).ThenBy(sc => sc.Name),
                        include: q => q.Include(sc => sc.VenomType)
                    );

                var response = symptomConfigs.Adapt<List<SymptomConfigResponse>>();
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all symptom configurations");
                return ApiResponseBuilder.CreateResponse<List<SymptomConfigResponse>>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

    }
}

