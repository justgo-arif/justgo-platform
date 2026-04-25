
namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTagLink : RecordInfo
    {
        public int AssetTagId { get; set; }
        public int TagId { get; set; }
        public int AssetId { get; set; }

    }

}
