using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.Workflows.Commands.WorkflowSubmissions
{
    public class WorkflowSubmissionCommand : WorkflowSubmissionDTO, IRequest<WorkflowResponseDTO>
    {
        
    }
}
