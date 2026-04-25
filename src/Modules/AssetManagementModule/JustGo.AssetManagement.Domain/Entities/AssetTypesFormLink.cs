namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTypesFormLink : BaseEntity
    {
        public int FieldLinkId { get; set; }
        public int AssetTypeId { get; set; }
        public int FormId { get; set; }
  
    }

}
