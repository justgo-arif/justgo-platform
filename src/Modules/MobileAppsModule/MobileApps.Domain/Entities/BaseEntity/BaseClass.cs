using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.BaseEntity
{
    public class BasePagingClass
    {
        public int NextId { get; set; } = 0;
        public int DataSize { get; set; } = 100;
        public required string SortOrder { get; set; } = "ASC";

    }
}
