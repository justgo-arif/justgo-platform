using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Classes
{
    public class EligibilityQueryParam
    {
        public int MemberDocId { get; set; }
        public int ProductId { get; set; } = 0;
        public int OccurrenceId { get; set; }   
        public int AttendeeId { get; set; }     
    }
}
