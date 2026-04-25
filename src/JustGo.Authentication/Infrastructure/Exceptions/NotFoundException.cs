using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message = "Resource not found") : base(message) { }
    }
}
