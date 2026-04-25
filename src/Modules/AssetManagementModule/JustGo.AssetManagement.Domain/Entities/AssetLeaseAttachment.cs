using System;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetLeaseAttachment : BaseEntity
    {
        public int LeaseAttachmentId { get; set; }
        public int AssetLeaseId { get; set; }
        public string AttachmentPath { get; set; }
    }
}