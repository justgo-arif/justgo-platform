using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateResultCompetitionStatus;

public class UpdateResultCompetitionStatusCommand : IRequest<Result<bool>>
{
    public UpdateResultCompetitionStatusCommand(int statusId, int fileId)
    {
        StatusId = statusId;
        FileId = fileId;
    }

    public int StatusId { get; set; }
    public int FileId { get; set; }
}