using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class EntityExtensionField
    {
        public EntityExtensionField()
        {
            AllowedValues = new List<FieldValue>();
        }
        public int ExId { get; set; }
        public int Id { get; set; }
        public int FieldSetId { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public bool IsMultiValue { get; set; }
        public ExtensionFieldDataType DataType { get; set; }
        public List<FieldValue> AllowedValues { get; set; }
        public string MetaData { get; set; }
        public bool IsInUse { get; set; }
        public string SyncGuid { get; set; }
    }
    public class EntityExtensionFieldDto
    {
        private string _dataType;
        public EntityExtensionFieldDto()
        {
            AllowedValues = new List<FieldValueDto>();
        }
        public int ExId { get; set; }
        public int Id { get; set; }
        public int FieldSetId { get; set; }
        public string Name { get; set; }
        //public string Caption { get; set; }
        //public string Description { get; set; }
        public bool IsMultiValue { get; set; }
        private ExtensionFieldDataType DataType { get; set; }
        public string Type
        {
            get
            {
                // Always resolve the current enum name when getting the Type
                var enumName = Enum.GetName(typeof(ExtensionFieldDataType), DataType);
                return !string.IsNullOrEmpty(enumName) ? enumName : _dataType;
            }

            set
            {
                // Check if the provided value matches an enum name
                if (Enum.IsDefined(typeof(ExtensionFieldDataType), DataType))
                {
                    _dataType = Enum.GetName(typeof(ExtensionFieldDataType), DataType);
                }
                else
                {
                    // Fallback to the provided value
                    _dataType = value;
                }
            }
        }
        public List<FieldValueDto> AllowedValues { get; set; }
        //public string MetaData { get; set; }
        //public bool IsInUse { get; set; }
        //public string SyncGuid { get; set; }
    }
}
