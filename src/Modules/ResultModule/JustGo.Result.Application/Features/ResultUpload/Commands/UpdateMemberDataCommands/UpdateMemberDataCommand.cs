using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands;

public sealed class UpdateMemberDataCommand(IDictionary<string, string> dynamicProperties, int id, SportType sportType)
    : IRequest<Result<string>>
{
    public int Id { get; set; } = id;
    public SportType SportType { get; } = sportType;
    public IDictionary<string, string> DynamicProperties { get; set; } = dynamicProperties;
}