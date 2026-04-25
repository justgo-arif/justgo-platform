using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class AssetSurchargeDTOV2:AssetSurchargeDTO
    {
        public string Type { get; set; }
        public int OwnerId { get; set; }
        public string ItemTag { get; set; }
    }
}
