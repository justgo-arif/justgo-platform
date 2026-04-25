using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetCredential : RecordInfo
    {
        public int AssetCredentialId { get; set; }
        public int AssetId { get; set; }
        public int CredentialDocId { get; set; }
        public int CredentialMasterDocId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int StatusId { get; set; }
    }

}
