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

        public async Task<ApiResponse<FirstAidGuidelineResponse>> CreateFirstAidGuidelineAsync(CreateFirstAidGuidelineRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request data cannot be null.");
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

                    var response = guideline.Adapt<FirstAidGuidelineResponse>();
                    return ApiResponseBuilder.BuildSuccessResponse(response, "First aid guideline created successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first aid guideline");
                return ApiResponseBuilder.BuildFailureResponse<FirstAidGuidelineResponse>(ex.Message);
            }
        }

        public async Task<ApiResponse<FirstAidGuidelineResponse>> GetFirstAidGuidelineByIdAsync(int id)
        {
            try
            {
                var guideline = await _unitOfWork.GetRepository<FirstAidGuideline>()
                    .GetByIdAsync(id);

                if (guideline == null)
                {
                    throw new NotFoundException($"First aid guideline with ID {id} not found.");
                }

                var response = guideline.Adapt<FirstAidGuidelineResponse>();
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first aid guideline by ID {Id}", id);
                return ApiResponseBuilder.BuildFailureResponse<FirstAidGuidelineResponse>(ex.Message);
            }
        }

        public async Task<ApiResponse<PagedData<FirstAidGuidelineResponse>>> GetFirstAidGuidelinesAsync(GetFirstAidGuidelineRequest request)
        {
            try
            {
                var query = _unitOfWork.GetRepository<FirstAidGuideline>()
                    .GetQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    query = query.Where(g => g.Name.Contains(request.Name));
                }

                if (request.Type.HasValue)
                {
                    query = query.Where(g => g.Type == request.Type.Value);
                }

                // Get paginated data
                var pagedData = await _unitOfWork.GetRepository<FirstAidGuideline>()
                    .GetPagingListAsync(
                        predicate: query.Expression,
                        orderBy: o => o.OrderByDescending(g => g.CreatedAt),
                        page: request.Page,
                        size: request.Size
                    );

                var response = new PagedData<FirstAidGuidelineResponse>
                {
                    Items = pagedData.Items.Adapt<List<FirstAidGuidelineResponse>>(),
                    TotalCount = pagedData.TotalCount,
                    Page = pagedData.Page,
                    Size = pagedData.Size,
                    TotalPages = pagedData.TotalPages
                };

                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first aid guidelines");
                return ApiResponseBuilder.BuildFailureResponse<PagedData<FirstAidGuidelineResponse>>(ex.Message);
            }
        }

        public async Task<ApiResponse<FirstAidGuidelineResponse>> UpdateFirstAidGuidelineAsync(int id, UpdateFirstAidGuidelineRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Request data cannot be null.");
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

                    if (!string.IsNullOrWhiteSpace(request.Content))
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

                    var response = guideline.Adapt<FirstAidGuidelineResponse>();
                    return ApiResponseBuilder.BuildSuccessResponse(response, "First aid guideline updated successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating first aid guideline with ID {Id}", id);
                return ApiResponseBuilder.BuildFailureResponse<FirstAidGuidelineResponse>(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteFirstAidGuidelineAsync(int id)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var guideline = await _unitOfWork.GetRepository<FirstAidGuideline>()
                        .GetByIdAsync(id);

                    if (guideline == null)
                    {
                        throw new NotFoundException($"First aid guideline with ID {id} not found.");
                    }

                    _unitOfWork.GetRepository<FirstAidGuideline>().Delete(guideline);
                    await _unitOfWork.CommitAsync();

                    return ApiResponseBuilder.BuildSuccessResponse(true, "First aid guideline deleted successfully!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting first aid guideline with ID {Id}", id);
                return ApiResponseBuilder.BuildFailureResponse<bool>(ex.Message);
            }
        }
    }
}
