using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class SelectListItemDTO<T>
    {
        public string Text {  get; set; }
        public T Value { get; set; }
    }
}
