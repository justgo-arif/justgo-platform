using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Event
{
    public class EventTicketType
    {
        public long DocId { get; set; }      
        public string Name { get; set; }      
        public long CourseDocId { get; set; } 
        public int IsEticket { get; set; }   
    }
}
