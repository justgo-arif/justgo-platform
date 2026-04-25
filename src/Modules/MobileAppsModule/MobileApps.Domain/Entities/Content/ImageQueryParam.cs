using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.Content
{
    public class ImageQueryParam
    {
        public int UserId { get; set; } = 1;
        public string ImagePath { get; set; }
        public string Gender { get; set; }
    }
}
