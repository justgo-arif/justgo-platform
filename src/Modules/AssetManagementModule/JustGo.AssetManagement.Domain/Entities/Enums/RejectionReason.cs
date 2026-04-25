using System.ComponentModel;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum RejectionReason
    {
        None = 0,

        [Description("Documentation Issues")]
        DocumentationIssues = 1,

        [Description("Data Inconsistencies")]
        DataInconsistencies = 2,

        [Description("Expired or Invalid Credentials")]
        ExpiredOrInvalidCredentials = 3,

        [Description("Media Issues")]
        MediaIssues = 4,

        [Description("Eligibility or Compliance Issue")]
        EligibilityOrComplianceIssue = 5,

        [Description("Incomplete or Incorrect Lease Information")]
        IncompleteOrIncorrectLeaseInformation = 11,

        [Description("Asset no longer required")]
        AssetNoLongerRequired = 16,

        [Description("Incorrect lease period or terms")]
        IncorrectLeasePeriodOrTerms = 17,

        [Description("Payment terms not acceptable")]
        PaymentTermsNotAcceptable = 18,

        [Description("Already leasing a similar asset")]
        AlreadyLeasingASimilarAsset = 19,

        [Description("Asset details are incorrect")]
        AssetDetailsAreIncorrect = 20,

        [Description("Duplicate or mistaken request")]
        DuplicateOrMistakenRequest = 21,

        [Description("Others")]
        Others = 100
    }
}
