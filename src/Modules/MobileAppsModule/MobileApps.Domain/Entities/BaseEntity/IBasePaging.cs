using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.BaseEntity
{
    public interface IBasePaging
    {
        public int NextId { get; set; }
        public int DataSize { get; set; }
        public string SortOrder { get; set; }
    }
}
