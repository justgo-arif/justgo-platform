using JustGo.Authentication.Helper.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById.SportTypeStrategies;

public interface IGetMemberDataQueryStrategyFactory
{
    IGetMemberDataQueryStrategy CreateStrategy(SportType sportType);
}

public class GetMemberDataQueryStrategyFactory : IGetMemberDataQueryStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<SportType, Type> _strategies;

    public GetMemberDataQueryStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _strategies = new Dictionary<SportType, Type>
        {
            { SportType.TableTennis, typeof(TableTennisQueryStrategy) },
            { SportType.Equestrian, typeof(EquestrianQueryStrategy) },
            { SportType.Gymnastics, typeof(EquestrianQueryStrategy) }
        };
    }

    public IGetMemberDataQueryStrategy CreateStrategy(SportType sportType)
    {
        if (!_strategies.TryGetValue(sportType, out var strategyType))
        {
            throw new NotSupportedException($"Sport type '{sportType}' is not supported.");
        }

        return (IGetMemberDataQueryStrategy)_serviceProvider.GetRequiredService(strategyType);
    }
}