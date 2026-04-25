using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.Common
{

    public class PermissionDTO
    {
        public bool CanView { get; set; }
        public bool CanApprove { get; set; }
        public bool CanPay { get; set; }
    }
}
