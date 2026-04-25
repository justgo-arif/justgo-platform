using JustGo.Authentication.Helper.Enums;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

public interface IResultProcessorFactory
{
    T GetProcessor<T>(SportType sportType, ResultProcessType processType);
}