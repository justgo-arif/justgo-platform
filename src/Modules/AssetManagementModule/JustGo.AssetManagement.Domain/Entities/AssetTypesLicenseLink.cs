using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTypesLicenseLink : BaseEntity
    {
        public int LicenseLinkId { get; set; }
        public int AssetTypeId { get; set; }
        public int LicenseDocId { get; set; }
        public LicenseType LicenseType { get; set; } 
        public AdditionalLicenseMode AdditionalLicenseMode { get; set; }
        public string LicenseConfig { get; set; }
        public bool IsUpgradable { get; set; }
        public int SourceUpgradeLicense { get; set; }
    }

}
