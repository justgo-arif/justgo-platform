namespace JustGo.FieldManagement.Domain.Entities
{
    public class EntityExtensionFieldSet
    {
        public int ExId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public List<EntityExtensionField> Fields { get; set; } = new List<EntityExtensionField>();
        public string MetaData { get; set; }
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
    }
}
