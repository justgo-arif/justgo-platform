using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById.SportTypeStrategies;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById;

public class GetMemberDataByIdQueryHandler : IRequestHandler<GetMemberDataByIdQuery, Result<object>>
{
    private readonly IGetMemberDataQueryStrategyFactory _strategyFactory;

    public GetMemberDataByIdQueryHandler(IGetMemberDataQueryStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory;
    }

    public async Task<Result<object>> Handle(GetMemberDataByIdQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            var strategy = _strategyFactory.CreateStrategy(request.SportType);
            
            return await strategy.ExecuteAsync(request, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            return Result<object>.Failure(ex.Message, ErrorType.BadRequest);
        }
        catch (Exception ex)
        {
            return Result<object>.Failure($"An error occurred while processing the request: {ex.Message}", ErrorType.InternalServerError);
        }
    }
}