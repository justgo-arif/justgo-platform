using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData.SportTypeStrategies;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData;

public class RevalidateMemberDataCommandHandler : IRequestHandler<RevalidateMemberDataCommand, Result<bool>>
{
    private readonly IRevalidateMemberDataStrategyFactory _strategyFactory;

    public RevalidateMemberDataCommandHandler(IRevalidateMemberDataStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory;
    }

    public async Task<Result<bool>> Handle(RevalidateMemberDataCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.FileId.HasValue)
        {
            if (request.OperationId is null)
            {
                return Result<bool>.Failure("OperationId must be provided when FileId is specified.", ErrorType.Validation);
            }

            if (request.MemberDataIds.Count > 0)
            {
                return Result<bool>.Failure("MemberDataIds must be empty when FileId is specified.", ErrorType.Validation);
            }
        }
        
        var handler = _strategyFactory.GetStrategy(request.SportType);
        return await handler.RevalidateMemberDataAsync(request.FileId, request.MemberDataIds, request.OperationId,
            cancellationToken);
    }
}