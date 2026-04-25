using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData;

public class RevalidateMemberDataCommand : IRequest<Result<bool>>
{
    public RevalidateMemberDataCommand(int? fileId, ICollection<int> memberDataIds, SportType sportType,
        string? operationId)
    {
        FileId = fileId;
        MemberDataIds = memberDataIds;
        SportType = sportType;
        OperationId = operationId;
    }

    public int? FileId { get; init; }
    public ICollection<int> MemberDataIds { get; init; }
    public SportType SportType { get; init; }
    public string? OperationId { get; init; }
}