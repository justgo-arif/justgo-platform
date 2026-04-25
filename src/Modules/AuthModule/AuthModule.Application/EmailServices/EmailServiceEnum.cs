using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.EmailServices  
{
    public enum EmailServiceEnum
    {
        Pending = 1,
        InProgress = 2,
        Complete = 3,
        Failed = 4,
        SendImmediately = 10
    }
}
