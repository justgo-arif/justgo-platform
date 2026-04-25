using JustGo.AssetManagement.Domain.Entities.Enums;


namespace JustGo.AssetManagement.Application.DTOs
{

    public class AssetOwnerDTO 
    {
        public string OwnerId { get; set; }
        public OwnerType OwnerTypeId { get; set; }

    }
}
