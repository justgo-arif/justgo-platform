using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Result.Domain.Entities
{
    public class ResultDiscipline
    {
        public int DisciplineId { get; set; }
        public string ResultEventTypeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Status { get; set; } = 0; 
    }
}

