using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule
{
    public class AssetGroupDiscountResultDTO
    {
        public decimal Amount { get; set; }
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
