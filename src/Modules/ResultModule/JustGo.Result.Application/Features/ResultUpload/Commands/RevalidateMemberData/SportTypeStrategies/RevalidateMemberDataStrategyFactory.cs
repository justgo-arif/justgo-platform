using JustGo.Authentication.Helper.Enums;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData.SportTypeStrategies;

public interface IRevalidateMemberDataStrategyFactory
{
    IRevalidateMemberDataStrategy GetStrategy(SportType sportType);
}

public class RevalidateMemberDataStrategyFactory : IRevalidateMemberDataStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<SportType, Type> _strategies;

    public RevalidateMemberDataStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _strategies = new Dictionary<SportType, Type>
        {
            { SportType.TableTennis, typeof(TableTennisRevalidateMemberDataStrategy) },
            { SportType.Equestrian, typeof(EquestrianRevalidateMemberDataStrategy) },
            { SportType.Gymnastics, typeof(EquestrianRevalidateMemberDataStrategy) }
        };
    }

    public IRevalidateMemberDataStrategy GetStrategy(SportType sportType)
    {
        if (_strategies.TryGetValue(sportType, out var strategyType))
        {
            if (_serviceProvider.GetService(strategyType) is IRevalidateMemberDataStrategy strategy)
            {
                return strategy;
            }

            throw new InvalidOperationException($"Strategy for sport type {sportType} is not registered properly.");
        }

        throw new NotSupportedException($"Sport type {sportType} is not supported.");
    }
}