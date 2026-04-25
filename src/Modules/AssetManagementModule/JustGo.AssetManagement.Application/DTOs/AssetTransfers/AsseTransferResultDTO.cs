using JustGo.AssetManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetTransfers
{

    public class AssetTransferResultDTO
    {
        public string AssetTransferId { get; set; }
        public DateTime TransferDate { get; set; }
        public string TransferStatus { get; set; }
        public string? TransferAttachmentId { get; set; }
        public string AttachmentName { get; set; }
        public string AssetName { get; set; }
        public string AssetReference { get; set; }
        public string AssetImage { get; set; }
        public string AssetImageId { get; set; }
        public string AssetRegisterId { get; set; }
        public int TransferDocCode { get; set; }
        public int AssetDocCode { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsTransferToAdmin { get; set; }
        public string InitiatedByUserId { get; set; }
        public string InitiatedByEmail { get; set; }
        public string InitiatedByFullName { get; set; }
        public string InitiatedByProfileImage { get; set; }
        public int InitiatedByDocId { get; set; }
        public string InitiatedByReferenceId { get; set; }
    }
}
