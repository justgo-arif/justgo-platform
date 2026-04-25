using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Notes
{
    public class NotesEntityTypeMapping
    {
        public int NotesEntityTypeMappingId { get; set; }
        public int EntityTypeId { get; set; }
        public string EntityTypeName { get; set; }
        public string TableName { get; set; }
        public string GuidColumn { get; set; }
        public string IdColumn { get; set; }
    }
}
