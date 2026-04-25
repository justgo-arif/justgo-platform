using Asp.Versioning;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/member-contact")]
[ApiController]
[Tags("Member Profile/Member Additional Contact")]
public class MemberContactController : ControllerBase
{
    IMediator _mediator;
    private readonly ICustomError _error;
    public MemberContactController(IMediator mediator, ICustomError error)
    {
        _mediator = mediator;
        _error = error;
    }

}
