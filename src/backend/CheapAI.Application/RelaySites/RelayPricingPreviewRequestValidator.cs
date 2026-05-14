using FluentValidation;

namespace CheapAI.Application.RelaySites;

public sealed class RelayPricingPreviewRequestValidator : AbstractValidator<RelayPricingPreviewRequest>
{
    public RelayPricingPreviewRequestValidator()
    {
        RuleFor(x => x.BaseUrl).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ProviderType)
            .NotEmpty()
            .Must(type => RelayPricingProviderType.All.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("价格源类型不合法");
        RuleFor(x => x.ApiKey).MaximumLength(512);
        RuleFor(x => x.RechargeRatio).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.BonusRatio).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.RateBaseline).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.GroupId).MaximumLength(128);
    }
}
