import 'package:flutter/material.dart';
import 'package:flutter_inappwebview/flutter_inappwebview.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/bookmark.dart';
import '../models/vpn_server.dart';
import '../services/bookmark_service.dart';
import '../services/security_service.dart';
import 'bookmarks_screen.dart';

class BrowserScreen extends StatefulWidget {
  const BrowserScreen({super.key});

  @override
  State<BrowserScreen> createState() => _BrowserScreenState();
}

class _BrowserScreenState extends State<BrowserScreen> {
  final GlobalKey webViewKey = GlobalKey();
  
  InAppWebViewController? webViewController;
  PullToRefreshController? pullToRefreshController;
  
  final TextEditingController urlController = TextEditingController();
  final BookmarkService bookmarkService = BookmarkService();
  final SecurityService securityService = SecurityService();
  
  String currentUrl = 'https://www.google.com';
  String pageTitle = 'Google';
  double progress = 0;
  bool isSecure = true;
  int adsBlocked = 0;
  bool adBlockEnabled = true;
  bool vpnConnected = false;
  VPNServer? selectedVPN;
  
  final List<String> adPatterns = [
    'doubleclick.net', 'googlesyndication.com', 'googleadservices.com',
    'youtube.com/api/stats/ads', 'google-analytics.com',
    '/ads/', 'advertising', 'adservice',
  ];

  @override
  void initState() {
    super.initState();
    
    pullToRefreshController = PullToRefreshController(
      settings: PullToRefreshSettings(color: const Color(0xFF1976D2)),
      onRefresh: () async => webViewController?.reload(),
    );
    
    _loadPreferences();
  }

  Future<void> _loadPreferences() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      adsBlocked = prefs.getInt('adsBlocked') ?? 0;
      adBlockEnabled = prefs.getBool('adBlockEnabled') ?? true;
    });
  }

  Future<void> _saveAdsBlocked() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt('adsBlocked', adsBlocked);
  }

  void _navigate(String input) {
    String url = input.trim();
    
    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      if (url.contains('.')) {
        url = 'https://$url';
      } else {
        url = 'https://www.google.com/search?q=${Uri.encodeComponent(url)}';
      }
    }

    if (url.contains('youtube.com/shorts/')) {
      final videoId = url.split('/shorts/')[1].split('?')[0];
      url = 'https://www.youtube.com/watch?v=$videoId';
    }

    webViewController?.loadUrl(urlRequest: URLRequest(url: WebUri(url)));
    FocusScope.of(context).unfocus();
  }

  Future<void> _addBookmark() async {
    if (currentUrl.isEmpty) return;
    
    final bookmark = Bookmark(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      title: pageTitle,
      url: currentUrl,
      createdAt: DateTime.now(),
    );
    
    await bookmarkService.addBookmark(bookmark);
    
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Bookmark added!'), duration: Duration(seconds: 1)),
      );
    }
  }

  void _showVPNDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('VPN Selection'),
        content: SizedBox(
          width: double.maxFinite,
          child: ListView.builder(
            shrinkWrap: true,
            itemCount: VPNServer.getServers().length,
            itemBuilder: (context, index) {
              final server = VPNServer.getServers()[index];
              return ListTile(
                leading: Text(server.flag, style: const TextStyle(fontSize: 24)),
                title: Text(server.country),
                subtitle: Text(server.speed),
                trailing: selectedVPN?.country == server.country
                    ? const Icon(Icons.check, color: Colors.green)
                    : null,
                onTap: () {
                  setState(() {
                    selectedVPN = server;
                    vpnConnected = true;
                  });
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Connected to ${server.country}')),
                  );
                },
              );
            },
          ),
        ),
        actions: [
          if (vpnConnected)
            TextButton(
              onPressed: () {
                setState(() {
                  vpnConnected = false;
                  selectedVPN = null;
                });
                Navigator.pop(context);
              },
              child: const Text('Disconnect'),
            ),
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('AKNOVA Browser', style: TextStyle(fontSize: 16)),
        actions: [
          IconButton(
            icon: Icon(adBlockEnabled ? Icons.shield : Icons.shield_outlined),
            color: adBlockEnabled ? Colors.green : Colors.white,
            onPressed: () {
              setState(() => adBlockEnabled = !adBlockEnabled);
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(adBlockEnabled ? 'Ad Blocker ON' : 'Ad Blocker OFF'),
                  duration: const Duration(seconds: 1),
                ),
              );
            },
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            margin: const EdgeInsets.only(right: 8),
            decoration: BoxDecoration(
              color: Colors.green,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text('$adsBlocked', 
              style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
          PopupMenuButton(
            itemBuilder: (context) => [
              PopupMenuItem(
                child: const ListTile(
                  leading: Icon(Icons.bookmarks),
                  title: Text('Bookmarks'),
                ),
                onTap: () {
                  Future.delayed(Duration.zero, () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => BookmarksScreen(
                          bookmarkService: bookmarkService,
                          onBookmarkTap: (url) {
                            webViewController?.loadUrl(
                              urlRequest: URLRequest(url: WebUri(url)),
                            );
                          },
                        ),
                      ),
                    );
                  });
                },
              ),
              PopupMenuItem(
                child: ListTile(
                  leading: Icon(vpnConnected ? Icons.vpn_lock : Icons.vpn_lock_outlined),
                  title: const Text('VPN'),
                ),
                onTap: () => Future.delayed(Duration.zero, _showVPNDialog),
              ),
              const PopupMenuItem(
                child: ListTile(
                  leading: Icon(Icons.info),
                  title: Text('About'),
                ),
              ),
            ],
          ),
        ],
      ),
      body: SafeArea(
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              color: Colors.grey[100],
              child: Row(
                children: [
                  Icon(isSecure ? Icons.lock : Icons.lock_open,
                    color: isSecure ? Colors.green : Colors.orange, size: 20),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextField(
                      controller: urlController,
                      decoration: const InputDecoration(
                        hintText: 'Search or enter URL...',
                        border: InputBorder.none,
                        contentPadding: EdgeInsets.symmetric(horizontal: 8),
                      ),
                      onSubmitted: _navigate,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.star_border, color: Colors.orange),
                    onPressed: _addBookmark,
                  ),
                  ElevatedButton(
                    onPressed: () => _navigate(urlController.text),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF1976D2),
                      foregroundColor: Colors.white,
                    ),
                    child: const Text('Go'),
                  ),
                ],
              ),
            ),
            if (progress < 1.0)
              LinearProgressIndicator(
                value: progress,
                backgroundColor: Colors.grey[200],
                valueColor: const AlwaysStoppedAnimation<Color>(Color(0xFF1976D2)),
              ),
            Expanded(
              child: InAppWebView(
                key: webViewKey,
                initialUrlRequest: URLRequest(url: WebUri(currentUrl)),
                initialSettings: InAppWebViewSettings(
                  javaScriptEnabled: true,
                  userAgent: 'Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36',
                ),
                pullToRefreshController: pullToRefreshController,
                onWebViewCreated: (controller) => webViewController = controller,
                onLoadStart: (controller, url) {
                  setState(() {
                    currentUrl = url.toString();
                    isSecure = url.toString().startsWith('https://');
                  });
                },
                onLoadStop: (controller, url) async {
                  pullToRefreshController?.endRefreshing();
                  setState(() {
                    currentUrl = url.toString();
                    urlController.text = url.toString();
                  });
                  
                  final title = await controller.getTitle();
                  setState(() => pageTitle = title ?? 'Page');
                  
                  _injectScripts();
                },
                onProgressChanged: (controller, progress) {
                  if (progress == 100) pullToRefreshController?.endRefreshing();
                  setState(() => this.progress = progress / 100);
                },
                shouldOverrideUrlLoading: (controller, navigationAction) async {
                  final url = navigationAction.request.url.toString();
                  
                  if (!securityService.isSafe(url)) {
                    _showSecurityWarning(url);
                    return NavigationActionPolicy.CANCEL;
                  }
                  
                  if (url.contains('youtube.com/shorts/')) {
                    final videoId = url.split('/shorts/')[1].split('?')[0];
                    controller.loadUrl(
                      urlRequest: URLRequest(
                        url: WebUri('https://www.youtube.com/watch?v=$videoId'),
                      ),
                    );
                    return NavigationActionPolicy.CANCEL;
                  }
                  
                  return NavigationActionPolicy.ALLOW;
                },
              ),
            ),
            Container(
              height: 50,
              decoration: const BoxDecoration(color: Color(0xFF263238)),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: [
                  IconButton(icon: const Icon(Icons.arrow_back, color: Colors.white),
                    onPressed: () => webViewController?.goBack()),
                  IconButton(icon: const Icon(Icons.arrow_forward, color: Colors.white),
                    onPressed: () => webViewController?.goForward()),
                  IconButton(icon: const Icon(Icons.home, color: Colors.white),
                    onPressed: () => webViewController?.loadUrl(
                      urlRequest: URLRequest(url: WebUri('https://www.google.com')))),
                  IconButton(icon: const Icon(Icons.refresh, color: Colors.white),
                    onPressed: () => webViewController?.reload()),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: vpnConnected ? Colors.green : Colors.grey,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(vpnConnected ? Icons.vpn_lock : Icons.vpn_lock_outlined,
                          size: 16, color: Colors.white),
                        const SizedBox(width: 4),
                        Text(vpnConnected ? selectedVPN!.flag : 'VPN',
                          style: const TextStyle(color: Colors.white, fontSize: 12)),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _injectScripts() {
    webViewController?.evaluateJavascript(source: '''
      if (window.location.hostname.includes('youtube.com')) {
        const style = document.createElement('style');
        style.textContent = 'ytd-reel-shelf-renderer,[is-shorts]{display:none!important}';
        style.id = 'aknova-blocker';
        if (!document.getElementById('aknova-blocker')) document.head.appendChild(style);
        
        document.addEventListener('click', (e) => {
          const link = e.target.closest('a');
          if (link?.href?.includes('/shorts/')) {
            e.preventDefault();
            const id = link.href.split('/shorts/')[1].split('?')[0];
            window.location.href = 'https://www.youtube.com/watch?v=' + id;
          }
        }, true);
      }
    ''');
  }

  void _showSecurityWarning(String url) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('⚠️ Security Warning'),
        content: Text(securityService.getThreatInfo(url)),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Go Back')),
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              webViewController?.loadUrl(urlRequest: URLRequest(url: WebUri(url)));
            },
            child: const Text('Proceed Anyway', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _saveAdsBlocked();
    urlController.dispose();
    super.dispose();
  }
}