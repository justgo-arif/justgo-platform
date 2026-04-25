using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Domain.Entities
{
    public class TermType
    {
        public int TermTypeId { get; set; }
        public string Name { get; set; }
    }
    public class TermRollingPeriod
    {
        public int TermRollingPeriodId { get; set; }
        public string Name { get; set; }
    }
    
}
