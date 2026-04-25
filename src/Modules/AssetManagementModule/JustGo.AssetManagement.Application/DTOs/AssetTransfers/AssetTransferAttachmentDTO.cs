using JustGo.AssetManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetTransfers
{
    public class AssetTransferAttachmentDTO
    {
        public string AttachmentName { get; set; }
        public string TransferAttachmentId { get; set; }
        public int AssetTransferId { get; set; }
        public string RecordGuid { get; set; }
    }
}
