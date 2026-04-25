using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetTypeDto
    {
        public string TypeId { get; set; }
        public string TypeName { get; set; }
        public string AssetApprovalConfig { get; set; }
        public AssetRegistrationConfig AssetRegistrationConfig { get; set; }
        public string AssetRetentionConfig { get; set; }
        public AssetLeaseConfig AssetLeaseConfig { get; set; }
        public AssetTransferConfig AssetTransferConfig { get; set; }
        public int? DigitalWalletTemplateId { get; set; }
        public AssetTypeConfig AssetTypeConfig { get; set; }
    }

    public class AssetTypeMetadataDto
    {
        public string AssetTypeId { get; set; }
        public string Name { get; set; }
    }
}
