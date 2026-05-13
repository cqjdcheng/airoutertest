using FluentValidation;

namespace CheapAI.Application.Models;

public sealed class UpdateModelStatusRequestValidator : AbstractValidator<UpdateModelStatusRequest>
{
    public UpdateModelStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => ModelStatusValue.All.Contains(status))
            .WithMessage("模型状态不合法");
    }
}
