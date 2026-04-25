using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.PaymentAccount;

public class StripeMerchantProfileDTO
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public bool IsActive { get; set; }
    public string? Tag { get; set; }
}
