using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{

    public class CredentialItemMetadata
    {
        public string SyncId { get; set; }
        public int DocId { get; set; }
        public string CredentialCode { get; set; }
        public string CredentialName { get; set; }
        public string ShortName { get; set; }
        public string CredentialCategory { get; set; }     
        public bool EnableCredentialJourney { get; set; }
        public bool Enablecredentialpayment { get; set; }
        public string CredentialDescription { get; set; }

    }

}
