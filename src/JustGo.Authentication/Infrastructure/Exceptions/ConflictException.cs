using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException(string message = "Record already exists") : base(message) { }
    }
}
