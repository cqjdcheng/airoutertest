using FluentValidation;

namespace CheapAI.Application.RelaySites;

public sealed class UpdateRelaySiteRequestValidator : AbstractValidator<UpdateRelaySiteRequest>
{
    public UpdateRelaySiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.BaseUrl).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Slug).MaximumLength(128);
        RuleFor(x => x.WebsiteUrl).MaximumLength(255);
        RuleFor(x => x.DocsUrl).MaximumLength(255);
        RuleFor(x => x.InviteUrl).MaximumLength(255);
        RuleFor(x => x.RecentReview).MaximumLength(1000);
    }
}
