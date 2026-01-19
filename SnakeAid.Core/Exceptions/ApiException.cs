using System.Net;

namespace SnakeAid.Core.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Reason { get; }

    public ApiException(string reason, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(reason)
    {
        StatusCode = statusCode;
        Reason = reason;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string reason)
        : base(reason, HttpStatusCode.NotFound)
    {
    }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string reason)
        : base(reason, HttpStatusCode.BadRequest)
    {
    }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string reason)
        : base(reason, HttpStatusCode.Unauthorized)
    {
    }
}

public class BusinessException : ApiException
{
    public BusinessException(string reason)
        : base(reason, HttpStatusCode.BadRequest)
    {
    }
}

public class ValidationException : ApiException
{
    public List<string> Errors { get; }

    public ValidationException(string reason, List<string>? errors = null)
        : base(reason, HttpStatusCode.UnprocessableEntity)
    {
        Errors = errors ?? new List<string>();
    }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string reason)
        : base(reason, HttpStatusCode.Forbidden)
    {
    }
}

public class ConflictException : ApiException
{
    public ConflictException(string reason)
        : base(reason, HttpStatusCode.Conflict)
    {
    }
}

public class TooManyRequestsException : ApiException
{
    public DateTime? RetryAfter { get; }
    public int? Limit { get; }
    public string? Period { get; }
    public string? Endpoint { get; }

    public TooManyRequestsException(string reason, DateTime? retryAfter = null)
        : base(reason, HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
    }

    public TooManyRequestsException(string reason, DateTime? retryAfter, int? limit = null, string? period = null, string? endpoint = null)
        : base(reason, HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
        Limit = limit;
        Period = period;
        Endpoint = endpoint;
    }
}