namespace JustGo.FieldManagement.Domain.Entities
{

    public class EntityExtensionField
    {
        public int ExId { get; set; }
        public int Id { get; set; }
        public int FieldSetId { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public bool IsMultiValue { get; set; }
        public int DataType { get; set; }
        public ExtensionFieldDataType Type
        {
            get => (ExtensionFieldDataType)DataType;
            set => DataType = (int)value;
        }
        public List<EntityExtensionFieldValue> AllowedValues { get; set; } = new();
        public string MetaData { get; set; }
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
    }
}
