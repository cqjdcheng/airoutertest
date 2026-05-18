using FluentValidation;

namespace CheapAI.Application.Models;

public sealed class ModelImportPreviewRequestValidator : AbstractValidator<ModelImportPreviewRequest>
{
    public ModelImportPreviewRequestValidator()
    {
        RuleFor(x => x.BaseUrl).MaximumLength(255);
        RuleFor(x => x.Vendor).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ApiKey).MaximumLength(512);
    }
}

public sealed class ImportModelsRequestValidator : AbstractValidator<ImportModelsRequest>
{
    public ImportModelsRequestValidator()
    {
        RuleFor(x => x.Models).NotEmpty();
        RuleForEach(x => x.Models).SetValidator(new ImportModelItemRequestValidator());
    }
}

public sealed class ImportModelItemRequestValidator : AbstractValidator<ImportModelItemRequest>
{
    public ImportModelItemRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ProviderId.HasValue || !string.IsNullOrWhiteSpace(x.ProviderSlug) || !string.IsNullOrWhiteSpace(x.Vendor))
            .WithMessage("请选择模型提供商");
        RuleFor(x => x.ProviderId).GreaterThan(0UL).When(x => x.ProviderId.HasValue);
        RuleFor(x => x.ProviderSlug).MaximumLength(64);
        RuleFor(x => x.ProviderName).MaximumLength(128);
        RuleFor(x => x.Slug).MaximumLength(128);
        RuleFor(x => x.Vendor).MaximumLength(64);
        RuleFor(x => x.OfficialModelId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.RequestName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ApiType)
            .NotEmpty()
            .Must(apiType => ModelApiTypeValue.All.Contains(apiType))
            .WithMessage("Model apiType is invalid.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CapabilitySource).MaximumLength(64);
        RuleFor(x => x.CapabilityScore).InclusiveBetween(0, 100).When(x => x.CapabilityScore.HasValue);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999);
        RuleFor(x => x.Status)
            .Must(status => ModelStatusValue.All.Contains(status))
            .WithMessage("模型状态不合法");
    }
}
