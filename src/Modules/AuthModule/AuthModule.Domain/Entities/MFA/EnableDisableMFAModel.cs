using System.ComponentModel.DataAnnotations;

namespace AuthModule.Domain.Entities.MFA;

public class EnableDisableMFAModel
{
    [Required]
    public Guid UserGuid { get; set; }
    [Required]
    public string AuthChannel { get; set; }
    [Required]
    public bool UpdateFlag { get; set; }
}
