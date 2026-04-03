namespace SnakeAid.Core.Constants;

public static class WithdrawalErrorCodes
{
    public const string WithdrawalNotFound = "WITHDRAWAL_NOT_FOUND";
    public const string WithdrawalForbidden = "WITHDRAWAL_FORBIDDEN";
    public const string WithdrawalInvalidStatus = "WITHDRAWAL_INVALID_STATUS";
    public const string WithdrawalInsufficientBalance = "WITHDRAWAL_INSUFFICIENT_BALANCE";
    public const string WithdrawalDailyLimitExceeded = "WITHDRAWAL_DAILY_LIMIT_EXCEEDED";
    public const string WithdrawalBankBinMissing = "WITHDRAWAL_BANK_BIN_MISSING";
}
