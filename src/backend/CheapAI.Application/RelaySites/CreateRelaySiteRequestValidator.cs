using FluentValidation;

namespace CheapAI.Application.RelaySites;

public sealed class CreateRelaySiteRequestValidator : AbstractValidator<CreateRelaySiteRequest>
{
    public CreateRelaySiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.BaseUrl).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Slug).MaximumLength(128);
        RuleFor(x => x.TestApiKey).MaximumLength(2048);
        RuleFor(x => x.TestIntervalMinutes).InclusiveBetween(15, 10080);
        RuleForEach(x => x.Offers).SetValidator(new RelaySiteOfferUpsertRequestValidator());
    }
}
