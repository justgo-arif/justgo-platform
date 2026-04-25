using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetTransfers
{
    public class AssetTransferDTO
    {
        public string AssetRegisterId { get; set; }
        public List<TransferOwnerDTO> TransferOwners { get; set; }
        //public DateTime TransferDate { get; set; }
        public string TransferNote { get; set; }

        public List<AssetTransferAttachmentDTO> TransferAttachment { get; set; }
    }
}
