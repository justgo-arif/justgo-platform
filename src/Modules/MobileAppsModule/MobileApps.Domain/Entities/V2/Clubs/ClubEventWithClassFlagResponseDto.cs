using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Clubs
{
    public class ClubEventWithClassFlagResponseDto
    {
        public int DocId { get; set; }
        public Guid? SyncGuid { get; set; }
        public bool IsExistEvent { get; set; }
        public bool IsExistClass { get; set; }
    }
}
