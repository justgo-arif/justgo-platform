using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.LicenseDtos
{
    public class MemberLicenseDTO
    {
        public int DocId { get; set; }
        public int ProductId { get; set; }
        public int OwnerId { get; set; }
    }
}
