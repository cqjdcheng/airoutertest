using FluentValidation;

namespace CheapAI.Application.SiteSettings;

public sealed class UpdateSiteSettingsRequestValidator : AbstractValidator<UpdateSiteSettingsRequest>
{
    public UpdateSiteSettingsRequestValidator()
    {
        RuleFor(x => x.SiteName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SiteIconUrl).MaximumLength(512);
    }
}
