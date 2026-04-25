using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class AssetLicenseResultDTO
    {
        public string AssetLicenseId { get; set; }
        public string LicenseStatus { get; set; }
        public DateTime? EndDate { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public int LicenseDocId { get; set; }
        public int ProductDocId { get; set; }
        public bool IsUpgradable { get; set; }
    }
}
