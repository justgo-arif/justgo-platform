using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class PermissionResult
    {
        public int Id { get; set; }
        public string Permission { get; set; } = string.Empty;
    }
}
