using FluentValidation;

namespace CheapAI.Application.Participation;

public sealed class CreateSelfTestRequestValidator : AbstractValidator<CreateSelfTestRequest>
{
    public CreateSelfTestRequestValidator()
    {
        RuleFor(x => x.SiteUrl)
            .NotEmpty()
            .MaximumLength(512)
            .Must(BeHttpUrl)
            .WithMessage("SiteUrl must be a valid http or https URL.");
        RuleFor(x => x.ModelName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.ApiType)
            .NotEmpty()
            .Must(value => value is "openai" or "anthropic")
            .WithMessage("ApiType must be openai or anthropic.");
        RuleFor(x => x.TestMode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ChallengeId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ChallengeAnswer).NotEmpty().MaximumLength(2048);
    }

    private static bool BeHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https";
    }
}
