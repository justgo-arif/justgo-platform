using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetLicenseCreateDto
    {
        public Guid AssetRegisterId { get; set; }
        public Guid LicenseId { get; set; }
        public Guid ProductId { get; set; }
    }
}
