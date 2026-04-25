using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Utilities
{
    public class Group
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Tag { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
