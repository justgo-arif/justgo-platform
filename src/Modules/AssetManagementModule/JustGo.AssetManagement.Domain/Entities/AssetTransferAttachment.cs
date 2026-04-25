using System;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTransferAttachment : BaseEntity
    {
        public int TransferAttachmentId { get; set; }
        public int AssetOwnershipTransferId { get; set; }
        public string AttachmentPath { get; set; }
    }
}