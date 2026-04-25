using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.FieldManagement.Domain.Entities
{
    public class CurrencyValueResult
    {
        public int fieldId { get; set; }
        public object Value { get; set; }
        public string Currency { get; set; }
    }
}
