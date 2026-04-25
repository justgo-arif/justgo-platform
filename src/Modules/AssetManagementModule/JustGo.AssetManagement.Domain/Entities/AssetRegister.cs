using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetRegister : RecordInfo
    {
        public int AssetId { get; set; }
        public int? AssetCategoryId { get; set; }
        public string AssetReference { get; set; }
        public string AssetName { get; set; }
        public int AssetTypeId { get; set; }
        public string AssetDescription { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public string Brand { get; set; }
        public string SerialNo { get; set; }
        public string Group { get; set; }
        public string AssetValue { get; set; }
        public int StatusId { get; set; }
        public DateTime IssueDate { get; set; }
        public string AssetConfig { get; set; }
        public string Country { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string PostCode { get; set; }
        public string Barcode { get; set; }

    }

}
