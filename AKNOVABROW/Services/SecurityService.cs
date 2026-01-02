using System.Collections.Generic;
using System.Linq;

namespace AKNOVABROW.Services
{
    public class SecurityService
    {
        private readonly HashSet<string> maliciousPatterns = new()
        {
            "malware", "spyware", "trojan", "virus", "phishing",
            "scam", "fraud", "hack", "exploit", "ransomware",
            "keylogger", "rootkit", "botnet", "spam"
        };

        private readonly HashSet<string> blockedDomains = new()
        {
            "malicious-site.com",
            "phishing-scam.net",
            "fake-bank.com"
        };

        public bool IsSafe(string url)
        {
            url = url.ToLower();

            // Check blocked domains
            if (blockedDomains.Any(domain => url.Contains(domain)))
                return false;

            // Check malicious patterns
            if (maliciousPatterns.Any(pattern => url.Contains(pattern)))
                return false;

            return true;
        }

        public string GetThreatInfo(string url)
        {
            if (!IsSafe(url))
                return "⚠️ THREAT DETECTED: This site may contain malware or phishing content!";

            return "✅ Site appears safe";
        }
    }
}
