using System.Collections.Generic;
using AKNOVABROW.Models;

namespace AKNOVABROW.Services
{
    public class VPNService
    {
        public List<VPNServer> GetAvailableServers()
        {
            return new List<VPNServer>
            {
                new VPNServer { Country = "United States", Flag = "🇺🇸", ProxyHost = "us-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Fast" },
                new VPNServer { Country = "United Kingdom", Flag = "🇬🇧", ProxyHost = "uk-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Fast" },
                new VPNServer { Country = "Germany", Flag = "🇩🇪", ProxyHost = "de-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Medium" },
                new VPNServer { Country = "France", Flag = "🇫🇷", ProxyHost = "fr-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Fast" },
                new VPNServer { Country = "Japan", Flag = "🇯🇵", ProxyHost = "jp-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Medium" },
                new VPNServer { Country = "Singapore", Flag = "🇸🇬", ProxyHost = "sg-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Fast" },
                new VPNServer { Country = "Australia", Flag = "🇦🇺", ProxyHost = "au-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Medium" },
                new VPNServer { Country = "Netherlands", Flag = "🇳🇱", ProxyHost = "nl-proxy.free-vpn.com", ProxyPort = 8080, Speed = "Fast" },
            };
        }

        public void Connect(VPNServer server)
        {
            // VPN connection logic here
            // For now, this is a placeholder
        }

        public void Disconnect()
        {
            // VPN disconnection logic
        }
    }
}
