using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Messages.Notifications;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class FirebaseNotificationService : IFirebaseNotificationService
{
    private static readonly object FirebaseLock = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<FirebaseNotificationService> _logger;
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

    public FirebaseNotificationService(
        IConfiguration configuration,
        ILogger<FirebaseNotificationService> logger,
        IUnitOfWork<SnakeAidDbContext> unitOfWork)
    {
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var storedToken = await _unitOfWork
            .GetRepository<Account>()
            .FirstOrDefaultAsync(
                selector: a => a.FcmToken,
                predicate: a => a.Id == message.UserId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(storedToken))
        {
            _logger.LogWarning(
                "Skipped notification {NotificationId} because no device token exists for user {UserId}",
                message.NotificationId,
                message.UserId);
            return;
        }

        if (!TryEnsureFirebaseApp())
        {
            _logger.LogError(
                "Skipped notification {NotificationId} because Firebase app is not configured",
                message.NotificationId);
            return;
        }

        var dataPayload = new Dictionary<string, string>(message.Data ?? new Dictionary<string, string>())
        {
            ["notificationType"] = message.Type
        };

        if (!string.IsNullOrWhiteSpace(message.DeepLink))
        {
            dataPayload["deepLink"] = message.DeepLink;
        }

        var pushMessage = new MulticastMessage
        {
            Tokens = new List<string> { storedToken },
            Notification = new Notification
            {
                Title = message.Title,
                Body = message.Body
            },
            Data = dataPayload
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(pushMessage, cancellationToken);

        if (response.FailureCount > 0)
        {
            for (var i = 0; i < response.Responses.Count; i++)
            {
                var sendResponse = response.Responses[i];
                if (sendResponse.IsSuccess)
                {
                    continue;
                }

                var ex = sendResponse.Exception;
                _logger.LogWarning(
                    ex,
                    "Firebase send failed for notification {NotificationId}, UserId={UserId}, Token={TokenMasked}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    message.NotificationId,
                    message.UserId,
                    MaskToken(storedToken),
                    ex is FirebaseMessagingException fmEx ? fmEx.MessagingErrorCode : null,
                    ex?.Message);

                if (ex is FirebaseMessagingException firebaseEx
                    && (firebaseEx.MessagingErrorCode == MessagingErrorCode.Unregistered
                        || firebaseEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument))
                {
                    await ClearUserTokenAsync(message.UserId, cancellationToken);
                }
            }
        }

        _logger.LogInformation(
            "Firebase notification {NotificationId} sent. Success={SuccessCount}, Failed={FailureCount}, UserId={UserId}, Type={Type}",
            message.NotificationId,
            response.SuccessCount,
            response.FailureCount,
            message.UserId,
            message.Type);
    }

    private bool TryEnsureFirebaseApp()
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            return true;
        }

        var credentialPath = _configuration["Firebase:CredentialPath"]
            ?? Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_PATH");

        if (string.IsNullOrWhiteSpace(credentialPath))
        {
            _logger.LogWarning("Firebase credential path is missing (Firebase:CredentialPath or FIREBASE_CREDENTIAL_PATH)");
            return false;
        }

        if (!Path.IsPathRooted(credentialPath))
        {
            credentialPath = Path.Combine(AppContext.BaseDirectory, credentialPath);
        }

        if (!File.Exists(credentialPath))
        {
            _logger.LogWarning("Firebase credential file was not found at {CredentialPath}", credentialPath);
            return false;
        }

        lock (FirebaseLock)
        {
            if (FirebaseApp.DefaultInstance != null)
            {
                return true;
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialPath)
            });

            _logger.LogInformation("Firebase app initialized using credentials at {CredentialPath}", credentialPath);
            return true;
        }
    }

    private async Task ClearUserTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork
            .GetRepository<Account>()
            .FirstOrDefaultAsync(
                predicate: a => a.Id == userId,
                asNoTracking: false,
                cancellationToken: cancellationToken);

        if (account == null || string.IsNullOrWhiteSpace(account.FcmToken))
        {
            return;
        }

        account.FcmToken = null;
        _unitOfWork.GetRepository<Account>().Update(account);
        await _unitOfWork.CommitAsync();

        _logger.LogWarning("Cleared invalid Firebase token for user {UserId}", userId);
    }

    private static string MaskToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length <= 10)
        {
            return "***";
        }

        return $"{token[..6]}...{token[^4..]}";
    }
}
