using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class EntityExtensionSchema
    {
        public EntityExtensionSchema()
        {
            Fields = new List<EntityExtensionField>();
            UiComps = new List<ExtensionUI>();
            FieldSets = new List<EntityExtensionFieldSet>();
        }

        public int ExId { get; set; }
        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
        public string ExtensionArea { get; set; }
        public int ExtensionEntityId { get; set; }
        public List<EntityExtensionField> Fields { get; set; }
        public List<EntityExtensionFieldSet> FieldSets { get; set; }
        public List<ExtensionUI> UiComps { get; set; }
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
        public bool SaveSchema { get; set; }
    }
}
