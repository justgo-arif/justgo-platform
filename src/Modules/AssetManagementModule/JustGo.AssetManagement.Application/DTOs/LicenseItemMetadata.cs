using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{

    public class LicenseItemMetadata
    {
        public string SyncId { get; set; }
        public int RepositoryId { get; set; }
        public string RepositoryName { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public DateTime? RegisterDate { get; set; }
        public string Location { get; set; }
        public bool? IsLocked { get; set; }
        public string Status { get; set; }
        public string Tag { get; set; }
        public string Version { get; set; }
        public int? UserId { get; set; }
        public string CredentialCode { get; set; }
        public string CredentialName { get; set; }
        public int? DefaultLength { get; set; }
        public string CredentialValue { get; set; }
        public string ShortName { get; set; }
        public string OwnerEntity { get; set; }
        public string CredentialCategory { get; set; }
        public string OwningEntity { get; set; }
        public DateTime? CredentialStartDate { get; set; }
        public bool? AllowedFutureDates { get; set; }
        public int? ExpiryPeriod { get; set; }
        public bool? ExpiryDateOverridden { get; set; }
        public string AboutCredential { get; set; }
        public bool? HideAboutCredential { get; set; }
        public int? OwnerEntityId { get; set; }
        public bool? SpecifyExpiryPeriod { get; set; }
        public DateTime? CredentialEndDate { get; set; }
        public DateTime? CredentialStartDateStartingValue { get; set; }
        public string CreationPrerequisite { get; set; }
        public string RetentionPrerequisite { get; set; }
        public string ApprovalCondition { get; set; }
        public string CredentialSubcategory { get; set; }
        public string ApprovalOption { get; set; }
        public DateTime? CredentialEndDateValue { get; set; }
        public string Permission { get; set; }
        public int? RenewalWindow { get; set; }
        public bool? EnableCreationFromJourney { get; set; }
        public string ReportOutputPath { get; set; }
        public bool? StartDateOverridden { get; set; }
        public bool? AllowedPastEndDates { get; set; }
        public bool? EnableCredentialJourney { get; set; }
        public bool? EnableExpirePreviousCredentialIfExpireOrCancel { get; set; }
        public bool? EnableExpirePreviousActiveCredential { get; set; }
        public int? WalletTemplateId { get; set; }
        public bool? IsRequest { get; set; }
    }

}
