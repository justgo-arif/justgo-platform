using System.ComponentModel.DataAnnotations.Schema;

namespace JustGo.AssetManagement.Domain.Entities
{

    public class AssetLeaseAdditionalFee 
    {
    
        public int AssetLeaseFeeId { get; set; }
        public string Name { get; set; }
        public int FeeLinkType { get; set; }
        public int FeeLinkId { get; set; }
        public string DisplayName { get; set; }
        public int ProductId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }
        
    }
}