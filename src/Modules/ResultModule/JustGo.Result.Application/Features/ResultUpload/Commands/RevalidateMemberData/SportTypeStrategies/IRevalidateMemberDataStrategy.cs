namespace JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData.SportTypeStrategies;

public interface IRevalidateMemberDataStrategy
{
    Task<bool> RevalidateMemberDataAsync(int? fileId, ICollection<int> memberDataIds, string? operationId,
        CancellationToken cancellationToken);
}