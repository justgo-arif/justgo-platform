using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.Content
{
    public class EventWithClubImages
    {
        public long DocId { get; set; } = 1;
        public string ImagePath { get; set; }
        public string Location { get; set; } = "";
    }
}
