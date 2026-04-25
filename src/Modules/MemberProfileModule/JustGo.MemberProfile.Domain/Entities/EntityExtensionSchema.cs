namespace JustGo.MemberProfile.Domain.Entities
{
    public class EntityExtensionSchema
    {
        public int ExId { get; set; }
        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; }
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }

        public bool SaveSchema { get; set; }
    }
}
