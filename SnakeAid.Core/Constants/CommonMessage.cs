namespace SnakeAid.Core.Constants;

public class CommonMessage
{
    public const string NotFound = "Resource not found";
    public const string Unauthorized = "Unauthorized access";
    public const string BadRequest = "Bad request";
    public const string InternalServerError = "An unexpected error occurred";
    public const string ValidationError = "Validation error";
    public const string GeneralError = "An error occurred";
    public const string OperationSuccessful = "Operation completed successfully";
}

public class CommonExceptionMessage
{
    public const string UserNotFound = "User not found";
    public const string InvalidCredentials = "Invalid email or password";
    public const string EmailAlreadyInUse = "Email is already in use";
    public const string UsernameAlreadyInUse = "Username is already in use";
    public const string TokenExpired = "Token has expired";
    public const string InvalidToken = "Invalid token";
    public const string OtpSendFailed = "Failed to send OTP. Please try again later.";
    public const string OtpVerificationFailed = "OTP verification failed. Please try again later.";
    public const string OtpResendFailed = "Failed to resend OTP. Please try again later.";
    public const string OtpInvalid = "Invalid OTP";
    public const string OtpExpired = "OTP has expired";
    public const string OtpResendLimitExceeded = "OTP resend limit exceeded. Please try again later.";
}