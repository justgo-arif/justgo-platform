using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class AssetMetaDataMasterLicenseDTO
    {
        public int LicenseDocId { get; set; }
        public int ProductDocId { get; set; }
        public string Location { get; set; }
        public Guid LicenseId { get; set; } // From d.SyncGuid AS Id
        public Guid ProductId { get; set; }
        public string Reference { get; set; }
        public decimal Sequence { get; set; }
        public string LicenceOwner { get; set; }
        public string ProductName { get; set; }
       // public string ProductDescription { get; set; }
        //public string Category { get; set; }
        public string LicenseOwnerName { get; set; }



    }
}
