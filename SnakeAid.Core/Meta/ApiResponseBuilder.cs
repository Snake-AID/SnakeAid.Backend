using SnakeAid.Core.Constants;

namespace SnakeAid.Core.Meta;

public class ApiResponseBuilder
{
    // Success response - without data
    public static SuccessResponse<object> BuildSuccessResponse()
    {
        return new SuccessResponse<object>
        {
            Message = CommonMessage.OperationSuccessful,
            Data = null
        };
    }

    // Success response - simpler format with just message and data
    public static SuccessResponse<T> BuildSuccessResponse<T>(T? data,
        string message = CommonMessage.OperationSuccessful)
    {
        return new SuccessResponse<T>
        {
            Message = message,
            Data = data
        };
    }

    // Error response - with more detailed error information
    public static ErrorResponse BuildErrorResponse(
        string message = "An error occurred",
        string? reason = null,
        List<string>? errors = null)
    {
        return new ErrorResponse
        {
            Message = message,
            Reason = reason,
            Errors = errors ?? new List<string>()
        };
    }

    // For pagination, we'll use the success format
    public static SuccessResponse<PagedData<T>> BuildPageResponse<T>(
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

        return new SuccessResponse<PagedData<T>>
        {
            Message = message,
            Data = pagedData
        };
    }
}