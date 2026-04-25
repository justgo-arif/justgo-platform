using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AdditionalFieldsDTO
{
    public class FormFieldValue
    {
        public string FormName { get; set; }
        public string FieldId { get; set; }
        public string FieldCaption { get; set; }
        public string DataType { get; set; }
        public string DisplayType { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string SyncGuid { get; set; }
    }
}
