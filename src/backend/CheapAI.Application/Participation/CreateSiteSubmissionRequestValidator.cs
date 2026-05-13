using FluentValidation;

namespace CheapAI.Application.Participation;

public sealed class CreateSiteSubmissionRequestValidator : AbstractValidator<CreateSiteSubmissionRequest>
{
    public CreateSiteSubmissionRequestValidator()
    {
        RuleFor(x => x.SiteName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SiteUrl)
            .NotEmpty()
            .MaximumLength(512)
            .Must(BeHttpUrl)
            .WithMessage("SiteUrl must be a valid http or https URL.");
        RuleFor(x => x.Contact).MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
    }

    private static bool BeHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https";
    }
}
