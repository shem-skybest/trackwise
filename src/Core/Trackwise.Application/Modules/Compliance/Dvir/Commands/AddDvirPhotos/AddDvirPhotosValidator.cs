using FluentValidation;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

internal sealed class AddDvirPhotosValidator : AbstractValidator<AddDvirPhotosCommand>
{
    private const long MaxFileBytes = 15 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"]
    };

    public AddDvirPhotosValidator()
    {
        RuleFor(i => i.Files).NotEmpty().WithMessage("Add at least one photo.");
        RuleForEach(i => i.Files).ChildRules(file =>
        {
            file.RuleFor(f => f)
                .Must(IsAllowed)
                .WithMessage(f => $"'{f.FileName}' isn't a JPEG, PNG or WebP photo.");
            file.RuleFor(f => f.FileSizeBytes)
                .LessThanOrEqualTo(MaxFileBytes)
                .WithMessage(f => $"'{f.FileName}' is over 15 MB.");
        });
    }

    private static bool IsAllowed(FileUpload file) =>
        ContentTypesByExtension.TryGetValue(Path.GetExtension(file.FileName), out var types)
        && types.Contains(file.ContentType.ToLowerInvariant());
}
