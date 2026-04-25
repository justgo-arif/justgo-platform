using JustGo.Authentication.Helper.Enums;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadGymnastic;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadTt;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportEquestrianResult;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportGymnasticResult;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportTableTennisResult;
using JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands.UpdateEquestrianMemberData;
using JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands.UpdateTableTennisMemberData;
using JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands.UploadEquestrianResultFile;
using JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands.UploadTableTennisResultFile;
using JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions.GetEquestrianCompetitions;
using JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions.GetGymnasticCompetitions;
using JustGo.Result.Application.Features.ResultView.Queries.GetEvents.GetEquestrianEvents;
using JustGo.Result.Application.Features.ResultView.Queries.GetEvents.GetGymnasticEvents;
using JustGo.Result.Application.Features.ResultView.Queries.GetResults.GetEquestrianResults;
using JustGo.Result.Application.Features.ResultView.Queries.GetResults.GetGymnasticResults;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

public class ResultProcessorFactory : IResultProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    private readonly Dictionary<(SportType, ResultProcessType), Type> _processorTypes = new()
    {
        { (SportType.Equestrian, ResultProcessType.ProcessResult), typeof(EquestrianResultProcessor) },
        { (SportType.TableTennis, ResultProcessType.ProcessResult), typeof(TableTennisResultProcessor) },
        { (SportType.Gymnastics, ResultProcessType.ProcessResult), typeof(GymnasticResultProcessor) },
        
        { (SportType.Equestrian, ResultProcessType.UploadFile), typeof(UploadEaResultProcessor) },
        { (SportType.TableTennis, ResultProcessType.UploadFile), typeof(UploadTtResultProcessor) },
        { (SportType.Gymnastics, ResultProcessType.UploadFile), typeof(UploadEaResultProcessor) },
        
        { (SportType.Equestrian, ResultProcessType.UpdateMemberData), typeof(UpdateEaMemberDataProcessor)},
        { (SportType.TableTennis, ResultProcessType.UpdateMemberData), typeof(UpdateTtMemberDataProcessor)},
        { (SportType.Gymnastics, ResultProcessType.UpdateMemberData), typeof(UpdateEaMemberDataProcessor)},
        
        { (SportType.Equestrian, ResultProcessType.RetrieveCompetitions), typeof(EquestrianCompetitionQueryProcessor)},
        { (SportType.Gymnastics, ResultProcessType.RetrieveCompetitions), typeof(GymnasticCompetitionQueryProcessor)},
        
        { (SportType.Equestrian, ResultProcessType.RetrieveResults), typeof(EquestrianResultViewViewProcessor)},
        { (SportType.Gymnastics, ResultProcessType.RetrieveResults), typeof(GymnasticResultViewViewProcessor)},
        
        { (SportType.Equestrian, ResultProcessType.RetrieveEvents), typeof(EquestrianEventsQueryProcessor)},
        { (SportType.Gymnastics, ResultProcessType.RetrieveEvents), typeof(GymnasticEventsQueryProcessor)},
        
        { (SportType.Gymnastics, ResultProcessType.ConfirmUploadFile), typeof(ConfirmUploadFileGymnasticProcessor)},
        { (SportType.Equestrian, ResultProcessType.ConfirmUploadFile), typeof(ConfirmUploadFileEquestrianProcessor)},
        { (SportType.TableTennis, ResultProcessType.ConfirmUploadFile), typeof(ConfirmUploadFileTtProcessor)},
        
        
        
    };

    public ResultProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T GetProcessor<T>(SportType sportType, ResultProcessType processType)
    {
        if (!_processorTypes.TryGetValue((sportType, processType), out var type))
            throw new NotSupportedException($"No processor registered for {sportType}, {processType}");

        var processor = _serviceProvider.GetRequiredService(type);

        if (processor is T typedProcessor)
            return typedProcessor;

        throw new InvalidCastException($"Resolved processor does not implement {typeof(T).Name}");
    }


}