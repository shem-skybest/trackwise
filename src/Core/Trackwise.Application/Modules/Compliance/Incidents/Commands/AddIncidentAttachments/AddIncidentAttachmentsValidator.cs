using FluentValidation;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class AddIncidentAttachmentsValidator : AbstractValidator<AddIncidentAttachmentsCommand>
{
    private const long MaxFileBytes = 15 * 1024 * 1024;

    private static readonly DocumentType[] Types = [DocumentType.Photo, DocumentType.PoliceReport, DocumentType.Other];

    // Photos from Messenger or a pasted screenshot, and a scanned report as a PDF.
    private static readonly Dictionary<string, string[]> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"],
        [".pdf"] = ["application/pdf"]
    };

    public AddIncidentAttachmentsValidator()
    {
        RuleFor(i => i.Type).Must(t => Types.Contains(t))
            .WithMessage("Attach a photo, a police report or another paper.");
        RuleFor(i => i.Files).NotEmpty().WithMessage("Add at least one file.");
        RuleForEach(i => i.Files).ChildRules(file =>
        {
            file.RuleFor(f => f)
                .Must(IsAllowed)
                .WithMessage(f => $"'{f.FileName}' isn't a JPEG, PNG, WebP or PDF.");
            file.RuleFor(f => f.FileSizeBytes)
                .LessThanOrEqualTo(MaxFileBytes)
                .WithMessage(f => $"'{f.FileName}' is over 15 MB.");
        });
    }

    private static bool IsAllowed(FileUpload file) =>
        ContentTypesByExtension.TryGetValue(Path.GetExtension(file.FileName), out var types)
        && types.Contains(file.ContentType.ToLowerInvariant());
}
