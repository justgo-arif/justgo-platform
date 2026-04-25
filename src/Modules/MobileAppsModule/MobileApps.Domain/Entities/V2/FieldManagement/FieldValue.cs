using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class FieldValue
    {
        public int FieldId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string Lang { get; set; }
        public int Sequence { get; set; }
        public int FieldValueId { get; set; }
    }

    public class FieldValueDto
    {
        public int FieldId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        //public string Caption { get; set; }
        //public string Description { get; set; }
        //public string Lang { get; set; }
        //public int Sequence { get; set; }
        public int FieldValueId { get; set; }
    }
}
