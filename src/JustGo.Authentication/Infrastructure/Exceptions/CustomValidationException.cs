using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Exceptions
{
    public class CustomValidationException : Exception
    {
        public CustomValidationException(string message = "This field is required") : base(message) { }
    }
}
