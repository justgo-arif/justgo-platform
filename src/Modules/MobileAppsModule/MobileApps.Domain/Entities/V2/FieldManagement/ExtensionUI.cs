using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class ExtensionUI
    {
        public int ExId { get; set; }
        public int ItemId { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public string Config { get; set; }
        public int FieldId { get; set; }
        public int Sequence { get; set; }
        public string SyncGuid { get; set; }
    }
    public class ExtensionUIDto 
    {
        public int ExId { get; set; }
        public int ItemId { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public string Config { get; set; }
        public int FieldId { get; set; }
        //public int Sequence { get; set; }
        public string SyncGuid { get; set; }
    }
}
