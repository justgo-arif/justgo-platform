using System.ComponentModel.DataAnnotations;

namespace AuthModule.Domain.Entities.MFA;

public class EnableDisableMFAForAdminModel
{
    [Required]
    public Guid UserGuid { get; set; }

    [Required]
    public bool AppUpdateFlag { get; set; }

    [Required]
    public bool WhatsAppUpdateFlag { get; set; }

    [Required]
    public bool EmailAuthFlag { get; set; }

    [Required]
    public bool ByPassForceMFASetUpFlag { get; set; }
    
}
