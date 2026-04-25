using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities.MFA
{
    public class SetupEmailAuthenticator
    {
        public int UserId { get; set; }
        public string Email { get; set; }
    }
}
