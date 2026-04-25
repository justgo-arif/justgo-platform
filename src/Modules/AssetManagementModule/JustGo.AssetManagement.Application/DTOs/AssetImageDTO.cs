using JustGo.AssetManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetImageDTO 
    {
        public string AssetImage { get; set; }
        public bool IsPrimary { get; set; }
        public int AssetId { get; set; }
        public string? ImageId { get; set; }
        public int AssetImageId { get; set; }
    }
}
