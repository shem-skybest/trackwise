using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class ReadBookingRequestValidator : AbstractValidator<ReadBookingRequestQuery>
{
    public const int MaxAttachmentBytes = 10 * 1024 * 1024;
    public const int MaxMessageLength = 8000;

    private static readonly Dictionary<string, string> MediaTypeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".jfif"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf"
    };

    public ReadBookingRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Pick the client first.");
        RuleFor(x => x.ReceivedOn).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.Attachment is { Length: > 0 } || !string.IsNullOrWhiteSpace(x.MessageText))
            .WithMessage("Add a screenshot or paste the client's message.");

        RuleFor(x => x.Attachment!.Length)
            .LessThanOrEqualTo(MaxAttachmentBytes)
            .When(x => x.Attachment is not null)
            .WithMessage("The screenshot is over 10 MB.");

        RuleFor(x => x)
            .Must(HasAllowedType)
            .When(x => x.Attachment is { Length: > 0 })
            .WithMessage("Use a PNG, JPEG or WebP screenshot, or a PDF.");

        RuleFor(x => x.MessageText).MaximumLength(MaxMessageLength);
    }

    // The extension and the declared type must agree; either alone is easy to fake.
    private static bool HasAllowedType(ReadBookingRequestQuery query) =>
        MediaTypeByExtension.TryGetValue(Path.GetExtension(query.AttachmentFileName ?? ""), out var expected)
        && string.Equals(expected, query.AttachmentMediaType, StringComparison.OrdinalIgnoreCase);
}
