using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class EntityExtensionFieldSet
    {
        public EntityExtensionFieldSet()
        {
            Fields = new List<EntityExtensionField>();
        }

        public int ExId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public List<EntityExtensionField> Fields { get; set; }
        public string MetaData { get; set; }
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
    }

    public class EntityExtensionFieldSetDtoV2_1
    {
        public EntityExtensionFieldSetDtoV2_1()
        {
            Fields = new List<EntityExtensionFieldDto>();
        }

        //public int ExId { get; set; }
        //public int Id { get; set; }
        //public string Name { get; set; }
        //public string Caption { get; set; }
       // public string Description { get; set; }
        public List<EntityExtensionFieldDto> Fields { get; set; }
        //public string MetaData { get; set; }
        //public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
    }
}
