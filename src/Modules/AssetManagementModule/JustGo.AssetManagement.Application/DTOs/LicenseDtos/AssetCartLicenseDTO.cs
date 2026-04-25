using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class AssetCartLicenseDTO
    {
        public int ProductDocid { get; set; }
        public string ProductId { get; set; }
        public string LicenseId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
