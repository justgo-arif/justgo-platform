using JustGo.AssetManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetLeases
{

    //AssetReference AssetImage  AssetImageId AssetRegisterId
    public class AssetLeaseResultDTO
    {
        public string AssetLeaseId { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime LeaseEndDate { get; set; }
        public int DateRangeType { get; set; }
        public string LeaseStatus { get; set; }
        public string? LeaseAttachmentId { get; set; }
        public string AttachmentName { get; set; }
        public string AssetName { get; set; }
        public string AssetReference { get; set; }
        public string AssetImage { get; set; }
        public string AssetImageId { get; set; }
        public string AssetRegisterId { get; set; }
        public int LeaseDocCode { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsLeaseeAdmin { get; set; }
        public bool IsOwnerAdmin { get; set; }
        public bool IsLeaseeFamily { get; set; }
        public bool IsOwnerFamily { get; set; }
        public bool IsLeasee { get; set; }
        public bool IsOwner { get; set; }
        public int? OwnerClubId { get; set; }
    }
}
