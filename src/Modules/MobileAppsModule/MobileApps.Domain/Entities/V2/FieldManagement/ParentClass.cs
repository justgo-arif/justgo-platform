using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.FieldManagement
{
    public class ParentClass
    {
        public int ItemId { get; set; }
        public string SyncGuid { get; set; }
        public string FormName { get; set; }
        public List<ChildItem> Fields { get; set; } = new();
    }

    public class ChildItem
    {
        public int? ParentId { get; set; }
        public int FieldId { get; set; }
        public int? FieldSetId { get; set; }
        public string? FieldName { get; set; }
        public string? IsRequired { get; set; }
        public string? IsMultiSelect { get; set; }
        public List<ChildValue>? Values { get; set; } = new();
        public ExtensionFieldDataType? DataType { get; set; }
        public string? Type { get; set; }
        public string? Class { get; set; }
        public string? ClassShort { get; set; }
        public string? Config { get; set; }
        public string? Rules { get; set; }

    }
    public class ChildValue
    {
        public int FieldId { get; set; }
        public int? Sequence { get; set; }
        public string? Value { get; set; }
        public string? Key { get; set; }
    }

    //dto

    public class ParentClassDto
    {
        //public int ItemId { get; set; }
        public string SyncGuid { get; set; }
        public string FormName { get; set; }
        public List<ChildItemDto> Fields { get; set; } = new();
    }

    public class ChildItemDto
    {
        //public int ParentId { get; set; }
        public int FieldId { get; set; }
        //public int FieldSetId { get; set; }
        public string FieldName { get; set; }
        public string IsRequired { get; set; }
        public string IsMultiSelect { get; set; }
        public List<ChildValueDto> Values { get; set; } = new();
        //public ExtensionFieldDataType DataType { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
        public Dictionary<string,object> Config { get; set; }
        public Dictionary<string, object> Rules { get; set; }
    }
    public class ChildValueDto
    {
        public int FieldId { get; set; }
        public int? Sequence { get; set; }
        public string Value { get; set; }
    }

    public class AdditionalFieldValueDto
    {
        public int Id { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }
    }
}
