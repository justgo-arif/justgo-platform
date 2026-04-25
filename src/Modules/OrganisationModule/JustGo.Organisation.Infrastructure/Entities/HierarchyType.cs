using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Organisation.Domain.Entities
{
    public class HierarchyType
    {
        public int Id { get; set; }
        public string HierarchyTypeName { get; set; }
        public short LevelNo { get; set; }
        public bool IsShared { get; set; }
    }
}
