using FluentValidation;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetMemberCredentials;

public class GetMemberCredentialsQueryValidator : AbstractValidator<GetMemberCredentialsQuery>
{
    public GetMemberCredentialsQueryValidator()
    {
        RuleFor(x => x.UserGuid).NotNull().NotEmpty().WithMessage("User guid is required.");
    }
}