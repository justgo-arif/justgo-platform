using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.DTOs
{
    public class HashTextParam
    {
        public string PlainText { get; set; } = string.Empty;
        public string HashedText { get; set; } = string.Empty;
    }
}
