using FluentValidation;

namespace CheapAI.Application.RelaySites;

public sealed class UpdateRelaySiteStatusRequestValidator : AbstractValidator<UpdateRelaySiteStatusRequest>
{
    public UpdateRelaySiteStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => RelaySiteStatusValue.All.Contains(status))
            .WithMessage("站点状态不合法");
    }
}
