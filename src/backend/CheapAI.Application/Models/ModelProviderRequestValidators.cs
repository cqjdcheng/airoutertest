using FluentValidation;

namespace CheapAI.Application.Models;

public sealed class CreateModelProviderRequestValidator : AbstractValidator<CreateModelProviderRequest>
{
    public CreateModelProviderRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Slug).MaximumLength(64);
        RuleFor(x => x.WebsiteUrl).MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999);
        RuleFor(x => x.Status)
            .Must(status => ModelProviderStatusValue.All.Contains(status))
            .WithMessage("提供商状态不合法");
    }
}

public sealed class UpdateModelProviderRequestValidator : AbstractValidator<UpdateModelProviderRequest>
{
    public UpdateModelProviderRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Slug).MaximumLength(64);
        RuleFor(x => x.WebsiteUrl).MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999);
        RuleFor(x => x.Status)
            .Must(status => ModelProviderStatusValue.All.Contains(status))
            .WithMessage("提供商状态不合法");
    }
}

public sealed class UpdateModelProviderStatusRequestValidator : AbstractValidator<UpdateModelProviderStatusRequest>
{
    public UpdateModelProviderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => ModelProviderStatusValue.All.Contains(status))
            .WithMessage("提供商状态不合法");
    }
}
