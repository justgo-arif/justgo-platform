using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class AssetShoppingCartItemDTO
    {
        public int DocId { get; set; }
        public int ProductId { get; set; }
        public string PurchaseItemTag { get; set; }
    }
}
