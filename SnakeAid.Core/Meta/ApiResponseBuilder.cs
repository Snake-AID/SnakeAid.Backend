using SnakeAid.Core.Constants;
using System.Net;

namespace SnakeAid.Core.Meta;

public static class ApiResponseBuilder
{
    // Manual constructor for custom responses
    public static ApiResponse<T> CreateResponse<T>(
        T? data,
        bool isSuccess,
        string message,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? errorCode = null,
        Dictionary<string, string[]>? validationErrors = null)
    {
        return new ApiResponse<T>
        {
            StatusCode = (int)statusCode,
            Message = message,
            IsSuccess = isSuccess,
            Data = data,
            Error = !isSuccess ? new ClientErrorResponse
            {
                ErrorCode = errorCode,
                Timestamp = DateTime.UtcNow,
                ValidationErrors = validationErrors
            } : null
        };
    }

    // Success response - without data
    public static ApiResponse<object> BuildSuccessResponse(string message = CommonMessage.OperationSuccessful)
    {
        return new ApiResponse<object>
        {
            StatusCode = (int)HttpStatusCode.OK,
            Message = message,
            IsSuccess = true,
            Data = null,
            Error = null
        };
    }

    // Success response - with data
    public static ApiResponse<T> BuildSuccessResponse<T>(T? data, string message = CommonMessage.OperationSuccessful)
    {
        return new ApiResponse<T>
        {
            StatusCode = (int)HttpStatusCode.OK,
            Message = message,
            IsSuccess = true,
            Data = data,
            Error = null
        };
    }

    // Error response - with detailed error information
    public static ApiResponse<object> BuildErrorResponse(
        string message = "An error occurred",
        string? errorCode = null,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new ApiResponse<object>
        {
            StatusCode = (int)statusCode,
            Message = message,
            IsSuccess = false,
            Data = null,
            Error = new ClientErrorResponse
            {
                ErrorCode = errorCode,
                Timestamp = DateTime.UtcNow,
                ValidationErrors = validationErrors
            }
        };
    }

    // Validation error response
    public static ApiResponse<T> BuildValidationErrorResponse<T>(
        Dictionary<string, string[]> validationErrors,
        string message = "Validation failed")
    {
        return new ApiResponse<T>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = message,
            IsSuccess = false,
            Data = default,
            Error = new ClientErrorResponse
            {
                ErrorCode = "VALIDATION_ERROR",
                Timestamp = DateTime.UtcNow,
                ValidationErrors = validationErrors
            }
        };
    }

    // Paginated response
    public static ApiResponse<PagedData<T>> BuildPagedResponse<T>(
        IEnumerable<T> items,
        int totalPages,
        int currentPage,
        int pageSize,
        long totalItems,
        string message = "Data retrieved successfully")
    {
        var pagedData = new PagedData<T>
        {
            Items = items,
            Meta = new PaginationMeta
            {
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            }
        };

        return new ApiResponse<PagedData<T>>
        {
            StatusCode = (int)HttpStatusCode.OK,
            Message = message,
            IsSuccess = true,
            Data = pagedData,
            Error = null
        };
    }

    // Common error responses
    public static ApiResponse<object> BuildNotFoundResponse(string message = "Resource not found")
    {
        return BuildErrorResponse(message, "NOT_FOUND", null, HttpStatusCode.NotFound);
    }

    public static ApiResponse<object> BuildUnauthorizedResponse(string message = "Unauthorized")
    {
        return BuildErrorResponse(message, "UNAUTHORIZED", null, HttpStatusCode.Unauthorized);
    }

    public static ApiResponse<object> BuildForbiddenResponse(string message = "Forbidden")
    {
        return BuildErrorResponse(message, "FORBIDDEN", null, HttpStatusCode.Forbidden);
    }

    public static ApiResponse<object> BuildConflictResponse(string message = "Resource already exists")
    {
        return BuildErrorResponse(message, "CONFLICT", null, HttpStatusCode.Conflict);
    }

    public static ApiResponse<object> BuildInternalServerErrorResponse(string message = "Internal server error")
    {
        return BuildErrorResponse(message, "INTERNAL_SERVER_ERROR", null, HttpStatusCode.InternalServerError);
    }

    // Additional 2xx Success responses
    public static ApiResponse<T> BuildCreatedResponse<T>(T? data, string message = "Resource created successfully")
    {
        return new ApiResponse<T>
        {
            StatusCode = (int)HttpStatusCode.Created,
            Message = message,
            IsSuccess = true,
            Data = data,
            Error = null
        };
    }

    public static ApiResponse<object> BuildAcceptedResponse(string message = "Request accepted for processing")
    {
        return new ApiResponse<object>
        {
            StatusCode = (int)HttpStatusCode.Accepted,
            Message = message,
            IsSuccess = true,
            Data = null,
            Error = null
        };
    }

    public static ApiResponse<object> BuildNoContentResponse(string message = "No content")
    {
        return new ApiResponse<object>
        {
            StatusCode = (int)HttpStatusCode.NoContent,
            Message = message,
            IsSuccess = true,
            Data = null,
            Error = null
        };
    }

    // Additional 4xx Client Error responses  
    public static ApiResponse<object> BuildUnprocessableEntityResponse(
        string message = "Unprocessable entity",
        Dictionary<string, string[]>? validationErrors = null)
    {
        return BuildErrorResponse(message, "UNPROCESSABLE_ENTITY", validationErrors, HttpStatusCode.UnprocessableEntity);
    }

    public static ApiResponse<object> BuildTooManyRequestsResponse(string message = "Too many requests")
    {
        return BuildErrorResponse(message, "TOO_MANY_REQUESTS", null, HttpStatusCode.TooManyRequests);
    }

    // 5xx Server Error responses
    public static ApiResponse<object> BuildBadGatewayResponse(string message = "Bad gateway")
    {
        return BuildErrorResponse(message, "BAD_GATEWAY", null, HttpStatusCode.BadGateway);
    }

    public static ApiResponse<object> BuildServiceUnavailableResponse(string message = "Service unavailable")
    {
        return BuildErrorResponse(message, "SERVICE_UNAVAILABLE", null, HttpStatusCode.ServiceUnavailable);
    }
}