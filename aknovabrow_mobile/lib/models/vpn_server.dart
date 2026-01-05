class VPNServer {
  final String country;
  final String flag;
  final String speed;

  VPNServer({
    required this.country,
    required this.flag,
    required this.speed,
  });

  String get displayName => '$flag $country ($speed)';

  static List<VPNServer> getServers() => [
        VPNServer(country: 'United States', flag: '🇺🇸', speed: 'Fast'),
        VPNServer(country: 'United Kingdom', flag: '🇬🇧', speed: 'Fast'),
        VPNServer(country: 'Germany', flag: '🇩🇪', speed: 'Medium'),
        VPNServer(country: 'Japan', flag: '🇯🇵', speed: 'Fast'),
        VPNServer(country: 'Singapore', flag: '🇸🇬', speed: 'Fast'),
        VPNServer(country: 'France', flag: '🇫🇷', speed: 'Medium'),
        VPNServer(country: 'Netherlands', flag: '🇳🇱', speed: 'Fast'),
        VPNServer(country: 'Australia', flag: '🇦🇺', speed: 'Medium'),
      ];
}