using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Event
{
    public class TicketTypeQueryModel
    {
        public int Id { get; set; }
        public bool IsRecurring { get; set; } = false;
    }
}
