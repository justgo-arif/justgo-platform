using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Result.Domain.Entities
{
    public class ResultUploadedMemberData
    {
        public int UploadedMemberDataId { get; set; }
        public int UploadedFileId { get; set; }
        public int UploadedMemberId { get; set; }
        public string MemberData { get; set; } = string.Empty;
    }
}
