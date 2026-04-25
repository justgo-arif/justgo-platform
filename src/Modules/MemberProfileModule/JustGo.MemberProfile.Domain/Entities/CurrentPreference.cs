

namespace JustGo.MemberProfile.Domain.Entities
{
    public class CurrentPreference
    {
        public required string MemberSyncGuid { get; set; }
        public List<PreferenceMaster> PreferenceMasters { get; set; } = new List<PreferenceMaster>();

    }
    public class PreferenceMaster
    {
        public int OptInMasterId { get; set; }
        public string? OwnerType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public List<PreferencesGroup> PreferencesGroups { get; set; } = new List<PreferencesGroup>();
    }

    public class PreferencesGroup
    {
        public int OptInGroupId { get; set; }
        public int OptInGroupMasterId { get; set; }
        public required string OptInGroupName { get; set; }
        public string? OptInGroupDescription { get; set; }
        public int OptInGroupSequence { get; set; }
        public List<Preference> Preferences { get; set; } = new List<Preference>();
    }

    public class Preference
    {
        public int OptInId { get; set; }
        public int OptInGroupRefId { get; set; }
        public required string Caption { get; set; }
        public string? OptInName { get; set; }
        public string? OptInDescription { get; set; }
        public int OptInStatus { get; set; }
        public int OptInSequence { get; set; }
        public required bool Selected { get; set; }
    }



}
