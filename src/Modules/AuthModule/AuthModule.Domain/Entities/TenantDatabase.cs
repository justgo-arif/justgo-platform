using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities
{
    public class TenantDatabase
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string DBUserId { get; set; }
        public string DBPassword { get; set; }
        public string ServerLocation { get; set; }
        public bool IsReadDatabase { get; set; }
    }
}
