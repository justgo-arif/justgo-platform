using JustGo.AssetManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetLeases
{
    public class AssetLeaseAttachmentDTO:BaseEntity
    {
        public string AttachmentName { get; set; }
        public string LeaseAttachmentId { get; set; }
        public int AssetLeaseId { get; set; }
    }
}
