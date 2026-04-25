namespace JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs
{

    using System.Collections.Generic;

    public class AssetRegistrationConfig
    {
        public Steps Steps { get; set; }
        public List<string> Order {  get; set; }
    }

    public class Steps
    {
        public BasicDetailStep BasicDetail { get; set; }
        public AdditionalDetailsStep AdditionalDetails { get; set; }
        public CredentialStep Credential { get; set; }
        public LicenseStep License { get; set; }
        public AdditionalLicenseStep AdditionalLicense { get; set; }
    }

    // ------------------------- Step Classes -------------------------

    public class BasicDetailStep
    {
        public string LabelName { get; set; }
        public bool Visible { get; set; }
        public BasicDetailConfig Config { get; set; }
    }

    public class AdditionalDetailsStep
    {
        public string LabelName { get; set; }
        public bool Visible { get; set; }
        public AdditionalDetailsConfig Config { get; set; }
    }

    public class CredentialStep
    {
        public string LabelName { get; set; }
        public bool Visible { get; set; }
        public CredentialConfig Config { get; set; }
    }

    public class LicenseStep
    {
        public string LabelName { get; set; }
        public bool Visible { get; set; }
        public LicenseConfig Config { get; set; }
    }

    public class AdditionalLicenseStep
    {
        public string LabelName { get; set; }
        public bool Visible { get; set; }
        public AdditionalLicenseConfig Config { get; set; }
    }

    // ------------------------- Config Classes -------------------------

    public class BasicDetailConfig
    {
        public List<string> OptionalFields { get; set; }
        public List<string> HiddenFields { get; set; }
    }

    public class AdditionalDetailsConfig
    {
        public List<int> AdditionalRegistrationForms { get; set; }
    }

    public class CredentialConfig
    {
        public List<Credential> Credentials { get; set; }
    }

    public class Credential
    {
        public int DocId { get; set; }
        public string Guid { get; set; }
        public string Title { get; set; }
        public string Caption { get; set; }
        public bool Required { get; set; }
        public int Min { get; set; }

    }

    public class LicenseConfig
    {
        public Requirement Requirements { get; set; }
    }

    public class Requirement
    {
        public bool CoreLicenseRequired { get; set; }
        public bool SameOrginationRequired { get; set; }
        public int MaximumCoreAllowed { get; set; }
    }

    public class AdditionalLicenseConfig
    {
        public int MaximumAdditionalAllowed { get; set; }
        public int MinimumOptionalRequired { get; set; }
    }


}
