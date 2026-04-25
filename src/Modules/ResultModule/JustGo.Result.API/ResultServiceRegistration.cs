using System.Reflection;
using FluentValidation;
using JustGo.Authentication.Infrastructure.Behaviors;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadGymnastic;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadTt;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportEquestrianResult;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportGymnasticResult;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportTableTennisResult;
using JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData.SportTypeStrategies;
using JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands.UpdateEquestrianMemberData;
using JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands.UpdateTableTennisMemberData;
using JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands.UploadEquestrianResultFile;
using JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands.UploadTableTennisResultFile;
using JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById.SportTypeStrategies;
using JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions.GetEquestrianCompetitions;
using JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions.GetGymnasticCompetitions;
using JustGo.Result.Application.Features.ResultView.Queries.GetEvents.GetEquestrianEvents;
using JustGo.Result.Application.Features.ResultView.Queries.GetEvents.GetGymnasticEvents;
using JustGo.Result.Application.Features.ResultView.Queries.GetResults.GetEquestrianResults;
using JustGo.Result.Application.Features.ResultView.Queries.GetResults.GetGymnasticResults;
using JustGo.RuleEngine.Interfaces.ResultEntryValidation;
using JustGo.RuleEngine.Services.ResultEntryValidation;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.API
{
    public static class ResultServiceRegistration
    {
        public static IServiceCollection AddResultServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.Load("JustGo.Result.Application"));
                cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(Assembly.Load("JustGo.Result.Application"));
            services.AddTransient<IEntryValidation, EntryValidation>();
            
            services.AddScoped<IResultProcessorFactory, ResultProcessorFactory>();
            services.AddScoped<EquestrianResultProcessor>();
            services.AddScoped<TableTennisResultProcessor>();
            services.AddScoped<GymnasticResultProcessor>();
            services.AddScoped<UploadTtResultProcessor>();
            services.AddScoped<UploadEaResultProcessor>();
            services.AddScoped<UpdateEaMemberDataProcessor>();
            services.AddScoped<UpdateTtMemberDataProcessor>();
            services.AddScoped<EquestrianCompetitionQueryProcessor>();
            services.AddScoped<GymnasticCompetitionQueryProcessor>();
            services.AddScoped<EquestrianResultViewViewProcessor>();
            services.AddScoped<GymnasticResultViewViewProcessor>();
            services.AddScoped<GymnasticEventsQueryProcessor>();
            services.AddScoped<EquestrianEventsQueryProcessor>();
            services.AddScoped<ConfirmUploadFileGymnasticProcessor>();
            services.AddScoped<ConfirmUploadFileEquestrianProcessor>();
            services.AddScoped<ConfirmUploadFileTtProcessor>();
            
            services.AddScoped<IGetMemberDataQueryStrategyFactory, GetMemberDataQueryStrategyFactory>();
            services.AddScoped<TableTennisQueryStrategy>();
            services.AddScoped<EquestrianQueryStrategy>();
            
            services.AddScoped<IRevalidateMemberDataStrategyFactory, RevalidateMemberDataStrategyFactory>();
            services.AddScoped<TableTennisRevalidateMemberDataStrategy>();
            services.AddScoped<EquestrianRevalidateMemberDataStrategy>();

            return services;
        }
    }
}
