using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.Common
{

    public class SearchSegmentDTO
    {
        public string ColumnName { get; set; }
        public string? FieldId { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }   
        public string ConditionJoiner { get; set; }
    }


}
