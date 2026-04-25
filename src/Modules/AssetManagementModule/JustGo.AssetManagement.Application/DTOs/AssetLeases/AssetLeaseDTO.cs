using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetLeases
{
    public class AssetLeaseDTO
    {
        public Guid AssetRegisterId { get; set; }
        public Guid AssetTypeId { get; set; }
        public List<LeaseeDTO> LeaseOwners { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime LeaseEndDate { get; set; }
        public AssetLeaseDateRangeType DateRangeType { get; set; }
        public string Comments { get; set; }

        public List<AssetLeaseAttachmentDTO> LeaseAttachment { get; set; }
    }
}
