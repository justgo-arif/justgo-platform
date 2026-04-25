namespace JustGo.AssetManagement.Application.DTOs.AdditionalFieldsDTO
{
    public class Field
    {
        public string FieldId { get; set; }
        public string FieldCaption { get; set; }
        public string DataType { get; set; }
        public string DisplayType { get; set; }
        public List<FieldValue> FieldValues { get; set; }
    }

}
