using System.ComponentModel.DataAnnotations;

namespace AuthModule.Domain.Entities.MFA;

public class RemoveAuthenticatorModel
{
    [Required]
    public Guid UserGuid { get; set; }
    [Required]
    public string AuthChannel { get; set; }
}
