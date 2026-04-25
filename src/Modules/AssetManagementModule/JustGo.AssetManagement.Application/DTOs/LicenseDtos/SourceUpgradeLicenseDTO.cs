using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class SourceUpgradeLicenseDTO
    {
        public int ProductDocId { get; set; }
        public int LicenseDocId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal FeeValue { get; set; }
        public string ProductId { get; set; }
        public string Type { get; set; }
        public int SourceProductDocId { get; set; }
        public int SourceLicenseDocId { get; set; }
        public int OwnerId { get; set; }
    }
}
