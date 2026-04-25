using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel.DataAnnotations;

namespace AuthModule.Application.Features.MFA.Commands.Update;

public class EnableDisableMFACommand : IRequest<bool>
{
    [Required]
    public int UserId { get; set; }
    [Required]
    public string AuthChannel { get; set; }
    [Required]
    public bool UpdateFlag { get; set; }
}
