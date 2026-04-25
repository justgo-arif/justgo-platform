using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class FieldPermission
    {
        [JsonPropertyName("create")]
        public bool Create { get; set; }
        [JsonPropertyName("edit")]
        public bool Edit { get; set; }
        [JsonPropertyName("view")]
        public bool View { get; set; }
        [JsonPropertyName("delete")]
        public bool Delete { get; set; }
        [JsonPropertyName("add")]
        public bool Add { get; set; }
        [JsonPropertyName("subscribe")]
        public bool Subscribe { get; set; }
    }
}
