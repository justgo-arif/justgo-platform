namespace JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs
{

    public class AssetTypeConfig
    {
        public string ParentModule { get; set; } = "Assets Hub";
        public bool ApproveLicenseByOwnerOnly { get; set; }
        public bool AllowedMultiOwner { get; set; }
        public bool AllowDuplicateAssetName { get; set; }
        public List<string> EditEnabledStatuses { get; set; }
        public CoreFieldConfig CoreFieldConfig { get; set; }
        public List<string> AssetOwnerTiers { get; set; }
        public List<string> LicenseOwners { get; set; }
        public PermissionConfig Permission { get; set; }
        public List<SectionLabelConfigItem> SectionLabelConfig { get; set; }
        public List<string> RolesAllowedToViewAllAsset { get; set; } = new List<string>();
    }

    public class CoreFieldConfig
    {
        public List<LabelConfigItem> LabelConfig { get; set; }
        public List<AllowedValuesConfigItem> AllowedValuesConfig { get; set; }
    }

    public class LabelConfigItem
    {
        public string Field { get; set; }
        public string Label { get; set; }
    }

    public class AllowedValuesConfigItem
    {
        public string Field { get; set; }
        public List<string> Options { get; set; }
    }

    public class PermissionConfig
    {
        public List<string> View { get; set; }
        public List<string> Delete { get; set; }
        public List<string> Create { get; set; }
        public List<string> Update { get; set; }
        public List<string> Approve { get; set; }
    }

    public class SectionLabelConfigItem
    {
        public string Key { get; set; }
        public string Label { get; set; }
    }


}
