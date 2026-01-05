class SecurityService {
  final List<String> _maliciousPatterns = [
    'malware', 'spyware', 'trojan', 'virus', 'phishing',
    'scam', 'fraud', 'hack', 'exploit',
  ];

  final List<String> _blockedDomains = [
    'malicious-site.com',
    'phishing-scam.net',
  ];

  bool isSafe(String url) {
    final lowercaseUrl = url.toLowerCase();

    for (var domain in _blockedDomains) {
      if (lowercaseUrl.contains(domain)) return false;
    }

    for (var pattern in _maliciousPatterns) {
      if (lowercaseUrl.contains(pattern)) return false;
    }

    return true;
  }

  String getThreatInfo(String url) {
    if (!isSafe(url)) {
      return '⚠️ THREAT DETECTED: This site may contain malware or phishing content!';
    }
    return '✅ Site appears safe';
  }
}