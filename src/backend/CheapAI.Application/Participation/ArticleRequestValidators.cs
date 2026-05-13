using FluentValidation;

namespace CheapAI.Application.Participation;

public sealed class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        Configure(this);
    }

    internal static void Configure<T>(AbstractValidator<T> validator)
        where T : CreateArticleRequest
    {
        validator.RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(128)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must use lowercase letters, numbers, and hyphens.");
        validator.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        validator.RuleFor(x => x.Summary).MaximumLength(500);
        validator.RuleFor(x => x.ContentMd).NotEmpty().MaximumLength(20000);
        validator.RuleFor(x => x.Status)
            .Must(x => x is "draft" or "published" or "archived")
            .WithMessage("Status must be draft, published, or archived.");
    }
}

public sealed class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequest>
{
    public UpdateArticleRequestValidator()
    {
        CreateArticleRequestValidator.Configure(this);
    }
}

public sealed class ReviewSubmissionRequestValidator : AbstractValidator<ReviewSubmissionRequest>
{
    public ReviewSubmissionRequestValidator()
    {
        RuleFor(x => x.ReviewNote).MaximumLength(1000);
    }
}
