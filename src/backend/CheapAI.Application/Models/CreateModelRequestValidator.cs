using FluentValidation;

namespace CheapAI.Application.Models;

public sealed class CreateModelRequestValidator : AbstractValidator<CreateModelRequest>
{
    public CreateModelRequestValidator()
    {
        RuleFor(x => x.Vendor).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OfficialModelId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Status)
            .Must(status => ModelStatusValue.All.Contains(status))
            .WithMessage("模型状态不合法");
    }
}
