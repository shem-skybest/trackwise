using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trackwise.Application.Abstractions.Captcha;

namespace Trackwise.Infrastructure.Communications.Captcha;

internal sealed class GoogleRecaptchaService : ICaptchaService
{
    private const string ApiEndpoint = "https://www.google.com/recaptcha/api/siteverify";
    private readonly HttpClient httpClient = new();
    private readonly GoogleRecaptchaOptions options;
    private readonly ILogger<GoogleRecaptchaService> logger;

    public GoogleRecaptchaService(IOptions<GoogleRecaptchaOptions> options, ILogger<GoogleRecaptchaService> logger)
    {
        if (string.IsNullOrEmpty(options.Value.SecretKey))
        {
            throw new ArgumentException("Secret key is an empty string");
        }

        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<bool> VerifyCaptchaAsync(string captchaValue)
    {
        // The widget posts an empty token when the box was never ticked
        if (string.IsNullOrWhiteSpace(captchaValue))
        {
            return false;
        }

        var postQueries = new List<KeyValuePair<string, string>>
        {
            new("secret", options.SecretKey!),
            new("response", captchaValue)
        };

        var response = await httpClient.PostAsync(new Uri(ApiEndpoint), new FormUrlEncodedContent(postQueries));
        var recaptchaResponse = await response.Content.ReadFromJsonAsync<GoogleRecaptchaResponse>();

        if (recaptchaResponse is not { Success: true })
        {
            // "invalid-input-secret" here means the configured key pair is wrong, not the user
            logger.LogWarning("reCAPTCHA verification failed: {ErrorCodes}",
                string.Join(", ", recaptchaResponse?.ErrorCodes ?? []));
            return false;
        }

        return true;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
internal record GoogleRecaptchaResponse
{
    public bool Success { get; init; }
    public float Score { get; init; }
    public string? Action { get; init; }
    public string? HostName { get; init; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; init; }
}
