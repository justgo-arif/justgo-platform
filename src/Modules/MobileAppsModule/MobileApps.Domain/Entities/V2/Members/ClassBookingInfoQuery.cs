using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Members
{
    public class ClassBookingInfoQuery
    {
        public int SessionId { get; set; }
        public int MemberDocId { get; set; }
        public int ProductId { get; set; }
        public int AttendeeId { get; set; } = -1;
       
    }
}
