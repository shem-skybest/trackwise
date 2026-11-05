using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Notifications;

namespace Trackwise.Infrastructure.Communications.Services;

public class PushNotificationService : IPushNotificationService
{
    private const string DefaultKeyPath = "firebase-adminsdk-key.json";

    private readonly FirebaseApp? firebaseApp;
    private readonly ILogger<PushNotificationService> logger;

    public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        this.logger = logger;

        var keyPath = Path.GetFullPath(configuration["Firebase:AdminKeyPath"] ?? DefaultKeyPath);
        if (!File.Exists(keyPath))
        {
            logger.LogWarning("No Firebase admin key at {KeyPath}: push notifications are off", keyPath);
            return;
        }

        firebaseApp = FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(keyPath) });
        logger.LogInformation("Firebase push notifications are on (key {KeyPath})", keyPath);
    }

    public async Task SendNotificationAsync(
        string title,
        string body,
        string deviceToken,
        IReadOnlyDictionary<string, string>? data = null)
    {
        if (firebaseApp is null)
        {
            return;
        }

        try
        {
            var message = new Message
            {
                Token = deviceToken, Notification = new Notification { Title = title, Body = body }, Data = data
            };
            await FirebaseMessaging.GetMessaging(firebaseApp).SendAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError("Could not send a notification to device: {DeviceToken}\nRaised an exception: {Exception}",
                deviceToken, ex.ToString());
        }
    }
}
