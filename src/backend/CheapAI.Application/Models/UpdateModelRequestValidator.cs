using FluentValidation;

namespace CheapAI.Application.Models;

public sealed class UpdateModelRequestValidator : AbstractValidator<UpdateModelRequest>
{
    public UpdateModelRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Vendor).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OfficialModelId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Slug).MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Status)
            .Must(status => ModelStatusValue.All.Contains(status))
            .WithMessage("Model status is invalid.");
    }
}
