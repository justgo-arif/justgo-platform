using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.Common
{
    public class SortItemDTO
    {
        public string ColumnName { get; set; }
        public bool OrderByDesceding{ get; set; } = false;
    }
}
