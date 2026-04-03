using System.Net;

namespace SnakeAid.Core.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Reason { get; }
    public string? ErrorCode { get; }

    public ApiException(
        string reason,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? errorCode = null)
        : base(reason)
    {
        StatusCode = statusCode;
        Reason = reason;
        ErrorCode = errorCode;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string reason, string? errorCode = null)
        : base(reason, HttpStatusCode.NotFound, errorCode)
    {
    }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string reason, string? errorCode = null)
        : base(reason, HttpStatusCode.BadRequest, errorCode)
    {
    }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string reason, string? errorCode = null)
        : base(reason, HttpStatusCode.Unauthorized, errorCode)
    {
    }
}

public class BusinessException : ApiException
{
    public BusinessException(string reason, string? errorCode = null)
        : base(reason, HttpStatusCode.BadRequest, errorCode)
    {
    }
}

public class ExternalServiceException : ApiException
{
    public ExternalServiceException(string reason, Exception? innerException = null)
        : base(reason, HttpStatusCode.BadGateway)
    {
        if (innerException != null)
        {
            Data["InnerException"] = innerException.Message;
        }
    }
}

public class SignalRNotificationException : ApiException
{
    public SignalRNotificationException(string reason, Exception? innerException = null)
        : base(reason, HttpStatusCode.InternalServerError)
    {
        if (innerException != null)
        {
            Data["InnerException"] = innerException.Message;
        }
    }
}

public class ValidationException : ApiException
{
    public List<string> Errors { get; }
    public Dictionary<string, string[]> ValidationErrors { get; }

    public ValidationException(string reason, List<string>? errors = null, string? errorCode = null)
        : base(reason, HttpStatusCode.UnprocessableEntity, errorCode)
    {
        Errors = errors ?? new List<string>();
        ValidationErrors = new Dictionary<string, string[]>();
    }

    public ValidationException(string reason, Dictionary<string, string[]> validationErrors, string? errorCode = null)
        : base(reason, HttpStatusCode.UnprocessableEntity, errorCode)
    {
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
        Errors = validationErrors?.SelectMany(x => x.Value).ToList() ?? new List<string>();
    }

    public ValidationException(
        string reason,
        List<string>? errors,
        Dictionary<string, string[]>? validationErrors,
        string? errorCode = null)
        : base(reason, HttpStatusCode.UnprocessableEntity, errorCode)
    {
        Errors = errors ?? new List<string>();
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string reason, string? errorCode = null)
        : base(reason, HttpStatusCode.Forbidden, errorCode)
    {
    }
}

public class ConflictException : ApiException
{
    public ConflictException(string reason, string? errorCode = null)
        : base(reason, HttpStatusCode.Conflict, errorCode)
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

public class ConfigurationException : ApiException
{
    public ConfigurationException(string reason)
        : base(reason, HttpStatusCode.InternalServerError)
    {
    }
}

public class DatabaseSchemaMismatchException : ApiException
{
    public DatabaseSchemaMismatchException(string reason)
        : base(reason, HttpStatusCode.ServiceUnavailable)
    {
    }
}
