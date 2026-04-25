using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Helper.Paginations.Keyset
{
    public class KeysetPagedResult<T>
    {
        public List<T> Items { get; set; }= new List<T>();
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
        public int? LastSeenId { get; set; }
    }
}
