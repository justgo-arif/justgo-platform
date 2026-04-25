namespace JustGo.FieldManagement.Domain.Entities
{
    public class EntityExtensionFieldValue
    {
        public int FieldId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string Lang { get; set; }
        public int Sequence { get; set; }
        public int FieldValueId { get; set; }
    }
}
