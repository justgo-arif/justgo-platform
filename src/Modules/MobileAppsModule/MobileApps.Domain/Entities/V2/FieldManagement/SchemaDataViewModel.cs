using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class SchemaDataViewModel
    {
        public string SyncGuid { get; set; }
        public string FormName { get; set; }
        public List<AdditionalValueDto> Items { get; set; } = new();
    }
    public class AdditionalValueDto
    {
        public int Id { get; set; }
        //public string FieldName { get; set; }
        public string Value { get; set; }
    }
}
