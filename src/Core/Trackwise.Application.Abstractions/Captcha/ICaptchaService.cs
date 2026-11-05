using Trackwise.Application.Abstractions.Captcha;
namespace Trackwise.Application.Abstractions.Captcha;

public interface ICaptchaService
{
    Task<bool> VerifyCaptchaAsync(string captchaValue);
}
