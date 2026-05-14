using FluentValidation;

namespace CheapAI.Application.RelaySites;

public sealed class RelaySiteOfferUpsertRequestValidator : AbstractValidator<RelaySiteOfferUpsertRequest>
{
    private static readonly HashSet<string> AllowedStatuses = ["active", "hidden", "archived"];

    public RelaySiteOfferUpsertRequestValidator()
    {
        RuleFor(x => x.ModelSlug).MaximumLength(128);
        RuleFor(x => x.Vendor).MaximumLength(64);
        RuleFor(x => x.OfficialModelId).MaximumLength(128);
        RuleFor(x => x.DisplayName).MaximumLength(128);
        RuleFor(x => x.SourceType).NotEmpty().MaximumLength(24);
        RuleFor(x => x.Status)
            .Must(status => AllowedStatuses.Contains(status))
            .WithMessage("报价状态不合法");
        RuleFor(x => x.RechargeRatio).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.BonusRatio).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x)
            .Must(x => x.ModelId.HasValue || !string.IsNullOrWhiteSpace(x.OfficialModelId))
            .WithMessage("报价必须选择已有模型或填写 OfficialModelId");
    }
}
