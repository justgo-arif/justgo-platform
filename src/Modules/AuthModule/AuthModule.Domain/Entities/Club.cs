using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities
{
    public class Club
    {
        public string SyncGuid { get; set; }
        public int DocId { get; set; }
        public string ClubName { get; set; }
    }
}
