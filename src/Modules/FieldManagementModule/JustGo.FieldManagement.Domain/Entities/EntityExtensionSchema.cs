namespace JustGo.FieldManagement.Domain.Entities
{
    public class EntityExtensionSchema
    {
        public int ExId { get; set; }
        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; }
        public List<EntityExtensionField> Fields { get; set; } = new List<EntityExtensionField>();
        public List<EntityExtensionFieldSet> FieldSets { get; set; } = new List<EntityExtensionFieldSet>();
        public List<EntityExtensionUI> UiComps { get; set; } = new List<EntityExtensionUI>();
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
        public bool SaveSchema { get; set; }
    }
}
