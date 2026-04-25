using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel.DataAnnotations;

namespace AuthModule.Application.Features.MFA.Queries.VerifyMFALogin;

public class VerifyMFALoginQuery : IRequest<User>
{
    [Required]
    public string UserName { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public string AuthChannel { get; set; }

}
