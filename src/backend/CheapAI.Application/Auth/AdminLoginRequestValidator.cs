using FluentValidation;

namespace CheapAI.Application.Auth;

public sealed class AdminLoginRequestValidator : AbstractValidator<AdminLoginRequest>
{
    public AdminLoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
