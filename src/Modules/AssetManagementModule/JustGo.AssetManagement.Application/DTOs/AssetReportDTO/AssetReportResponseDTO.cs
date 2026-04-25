using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetReportDTO
{
    public class AssetReportResponseDTO: AssetReportDto
    {
        public string EntityId { get; set; }
        public int Id { get; set; }
        public int OwnerId { get; set; }
    }
}
