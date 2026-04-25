using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class NoteCategories
    {
        public int NoteCategoryId { get; set; }
        public string NoteCategoryName { get; set; } = string.Empty;
        public string NoteCategoryStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

}
