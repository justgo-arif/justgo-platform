using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V3.Classes
{
    public class PhotoConsent
    {
        public int UserId { get; set; }
        public bool IsPhotoConsent { get; set; }
    }
}
