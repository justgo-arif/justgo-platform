using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AdditionalFieldsDTO
{
    public class FormModel
    {
        public string FormName { get; set; }
        public List<Field> Fields { get; set; }
    }

}
