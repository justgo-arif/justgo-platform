using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel.DataAnnotations;

namespace AuthModule.Application.Features.MFA.Commands.Update;

public class EnableDisableMFAForAdminCommand : IRequest<bool>
{
    [Required]
    public int MemberDocId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public bool AppUpdateFlag { get; set; }
    [Required]
    public bool WhatsAppUpdateFlag { get; set; }
    [Required]
    public bool ByPassForceMFASetUpFlag { get; set; }
    [Required]
    public bool EmailAuthFlag { get; set; }
}
