using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class EntityExtensionSchemaCore
    {
        public int ExId { get; set; }
        public string OwnerType { get; set; }
        public string ExtensionArea { get; set; }
        public int ParentId { get; set; }
        public int Sequence { get; set; }
        public string Class { get; set; }
        public string Config { get; set; }
        public int FieldId { get; set; }
        public int ItemId { get; set; }
    }
}
