using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.Credential
{
    public class AssetCredentialRequestDTO
    {
        public Guid AssetRegisterId { get; set; }
        public Guid CredentialId { get; set; }
        public DateTime Granteddate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
