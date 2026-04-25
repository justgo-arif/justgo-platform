using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V3.Classes
{
    public class UserNoteCategoryStatus
    {
        public int UserId { get; set; }
        public bool IsAlert { get; set; }
        public bool IsMedical { get; set; }
    }
}
