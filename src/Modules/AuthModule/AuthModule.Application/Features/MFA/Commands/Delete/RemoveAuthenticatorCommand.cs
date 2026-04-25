using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel.DataAnnotations;

namespace AuthModule.Application.Features.MFA.Commands.Delete;

public class RemoveAuthenticatorCommand : IRequest<bool>
{
    [Required]
    public int UserId { get; set; }
    [Required]
    public string AuthChannel { get; set; }
}
