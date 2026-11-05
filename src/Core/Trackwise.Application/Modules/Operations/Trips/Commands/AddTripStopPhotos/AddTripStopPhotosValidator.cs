using FluentValidation;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class AddTripStopPhotosValidator : AbstractValidator<AddTripStopPhotosCommand>
{
    private const long MaxPhotoBytes = 10 * 1024 * 1024;

    // Only what a browser can show as a thumbnail: Messenger saves JPEG, a pasted screenshot is PNG.
    private static readonly Dictionary<string, string[]> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"]
    };

    public AddTripStopPhotosValidator()
    {
        RuleFor(i => i.StopIds).NotEmpty().WithMessage("Pick the stop the photos belong to.");
        RuleFor(i => i.Photos).NotEmpty().WithMessage("Add at least one photo.");
        RuleForEach(i => i.Photos).ChildRules(photo =>
        {
            photo.RuleFor(p => p)
                .Must(IsAllowedImage)
                .WithMessage(p => $"'{p.FileName}' isn't a JPEG, PNG or WebP photo.");
            photo.RuleFor(p => p.FileSizeBytes)
                .LessThanOrEqualTo(MaxPhotoBytes)
                .WithMessage(p => $"'{p.FileName}' is over 10 MB.");
        });
    }

    private static bool IsAllowedImage(FileUpload photo) =>
        ContentTypesByExtension.TryGetValue(Path.GetExtension(photo.FileName), out var types)
        && types.Contains(photo.ContentType.ToLowerInvariant());
}
