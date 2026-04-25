using FluentValidation;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMembers;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.AddFamilyMember;

public sealed class AddFamilyMemberCommandValidator : AbstractValidator<AddFamilyMemberCommand>
{

    private readonly IMediator _mediator;
    public AddFamilyMemberCommandValidator(IMediator mediator)
    {
        _mediator = mediator;
        RuleFor(x => x.UserSyncGuid)
            .NotEmpty();

        RuleFor(x => x.MemberSyncGuid)
            .NotEmpty();

        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
            {
                var message = await MemberDoesNotExistInFamilyAsync(command, ct);
                ct.ThrowIfCancellationRequested();
                return string.IsNullOrEmpty(message);
            })
            .WithMessage((command) => GetValidationMessage(command).Result);
    }


    private async Task<string> GetValidationMessage(AddFamilyMemberCommand command)
        => await MemberDoesNotExistInFamilyAsync(command, CancellationToken.None);

    public async Task<string> MemberDoesNotExistInFamilyAsync(AddFamilyMemberCommand command, CancellationToken cancellationToken)
    {
        var userFamilyMembers = await _mediator.Send(new GetFamilyMembersQuery(command.UserSyncGuid), cancellationToken);
        var memberFamilyMembers = await _mediator.Send(new GetFamilyMembersQuery(command.MemberSyncGuid), cancellationToken);

        var userFamilyIds = userFamilyMembers.Select(m => m.FamilyId).ToHashSet();
        var memberFamilyIds = memberFamilyMembers.Select(m => m.FamilyId).ToHashSet();

        if (memberFamilyIds.Overlaps(userFamilyIds))
        {
            return "Member already exists in this family";
        }

        if (memberFamilyIds.Any() && !memberFamilyIds.IsSubsetOf(userFamilyIds))
        {
            return "Member already exists in another family";
        }

        return string.Empty;
    }
}