using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class UiPermission
    {
        [JsonPropertyName("view")]
        public bool View { get; set; }
    }
}
