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
using System.Net;

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
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = false
                    };

                    var guideline = new FirstAidGuideline
                    {
                        Name = request.Name,
                        Content = System.Text.Json.JsonSerializer.Serialize(request.Content, jsonOptions),
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
                return ApiResponseBuilder.CreateResponse<FirstAidGuidelineResponse>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
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
                return ApiResponseBuilder.CreateResponse<FirstAidGuidelineResponse>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<PagedData<FirstAidGuidelineResponse>>> FilterFirstAidGuidelinesAsync(GetFirstAidGuidelineRequest request)
        {
            try
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

                var response = new PagedData<FirstAidGuidelineResponse>
                {
                    Items = pagedData.Items.Adapt<List<FirstAidGuidelineResponse>>(),
                    Meta = pagedData.Meta
                };

                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering first aid guidelines");
                return ApiResponseBuilder.CreateResponse<PagedData<FirstAidGuidelineResponse>>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
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

                    if (request.Content != null)
                    {
                        var jsonOptions = new System.Text.Json.JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            WriteIndented = false
                        };
                        guideline.Content = System.Text.Json.JsonSerializer.Serialize(request.Content, jsonOptions);
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
                return ApiResponseBuilder.CreateResponse<FirstAidGuidelineResponse>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
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
                return ApiResponseBuilder.CreateResponse<bool>(
                    false, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<List<FirstAidGuidelineResponse>>> GetAllFirstAidGuidelineAsync()
        {
            try
            {
                var guidelines = await _unitOfWork.GetRepository<FirstAidGuideline>()
                    .GetListAsync(orderBy: o => o.OrderBy(g => g.Name));

                var response = guidelines.Adapt<List<FirstAidGuidelineResponse>>();
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all first aid guidelines");
                return ApiResponseBuilder.CreateResponse<List<FirstAidGuidelineResponse>>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }

        public async Task<ApiResponse<List<FirstAidGuidelineResponse>>> GetFirstAidGuidelinesBySnakeSpeciesIdAsync(int snakeSpeciesId)
        {
            try
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

                if (!guidelines.Any())
                {
                    return ApiResponseBuilder.BuildSuccessResponse(
                        new List<FirstAidGuidelineResponse>(),
                        "No first aid guidelines found for this snake species."
                    );
                }

                var response = guidelines.Adapt<List<FirstAidGuidelineResponse>>();
                return ApiResponseBuilder.BuildSuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first aid guidelines for snake species ID {SnakeSpeciesId}", snakeSpeciesId);
                return ApiResponseBuilder.CreateResponse<List<FirstAidGuidelineResponse>>(
                    default, false, ex.Message, HttpStatusCode.BadRequest);
            }
        }
    }
}

