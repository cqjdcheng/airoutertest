using FluentValidation;

namespace CheapAI.Application.RelaySites;

public sealed class CreateRelaySiteRequestValidator : AbstractValidator<CreateRelaySiteRequest>
{
    public CreateRelaySiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.BaseUrl).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Slug).MaximumLength(128);
        RuleForEach(x => x.Offers).SetValidator(new RelaySiteOfferUpsertRequestValidator());
    }
}
