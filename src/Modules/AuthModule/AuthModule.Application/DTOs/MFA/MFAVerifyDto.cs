using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.DTOs.MFA 
{
    public class MFAVerifyDto
    {
        public string Status { get; set; }
        public string To { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string VerifyDate { get; set; }
    }
}
