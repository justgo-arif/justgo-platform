using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetCredentialDTO
    {
        public int DocId { get; set; }
        //public int RepositoryId { get; set; }
        //public string RepositoryName { get; set; }
        //public string Type { get; set; }
        //public string Title { get; set; }
        //public DateTime? RegisterDate { get; set; }
        //public string Location { get; set; }
        //public bool IsLocked { get; set; }
        //public int Status { get; set; }
        //public int Tag { get; set; }
        //public int Version { get; set; }
        //public int UserId { get; set; }
        public string AssetCredentialId { get; set; }
        public string MasterCredentialId { get; set; }
        public string Credentialname { get; set; }
        public string ShortName { get; set; }
        public string CredentialCode { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? Expirydate { get; set; }
        public int AssetStatusId { get; set; }
        public string StateName { get; set; }
        public string CredentialCategory { get; set; }
    }

}
