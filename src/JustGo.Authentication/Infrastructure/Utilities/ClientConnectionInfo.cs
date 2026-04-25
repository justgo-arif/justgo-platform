using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Utilities
{
    public class ClientConnectionInfo
    {
        public string IpAddress { get; set; } = "Unknown";
        public string Port { get; set; } = "Unknown";
        public string Source { get; set; } = "Unknown"; // How the IP was determined
        public bool IsProxied { get; set; }
        public List<string> ProxyChain { get; set; } = new List<string>();
    }
}
