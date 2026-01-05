import 'package:flutter/material.dart';
import '../models/bookmark.dart';
import '../services/bookmark_service.dart';

class BookmarksScreen extends StatefulWidget {
  final BookmarkService bookmarkService;
  final Function(String) onBookmarkTap;

  const BookmarksScreen({
    super.key,
    required this.bookmarkService,
    required this.onBookmarkTap,
  });

  @override
  State<BookmarksScreen> createState() => _BookmarksScreenState();
}

class _BookmarksScreenState extends State<BookmarksScreen> {
  List<Bookmark> bookmarks = [];
  bool isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadBookmarks();
  }

  Future<void> _loadBookmarks() async {
    setState(() => isLoading = true);
    final loaded = await widget.bookmarkService.getBookmarks();
    setState(() {
      bookmarks = loaded;
      isLoading = false;
    });
  }

  Future<void> _deleteBookmark(String id) async {
    await widget.bookmarkService.deleteBookmark(id);
    _loadBookmarks();
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Bookmark deleted')),
      );
    }
  }

  Future<void> _exportBookmarks() async {
    final json = await widget.bookmarkService.exportBookmarks();
    
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Export Bookmarks'),
        content: SingleChildScrollView(
          child: SelectableText(json),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  Future<void> _importBookmarks() async {
    final controller = TextEditingController();
    
    final result = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Import Bookmarks'),
        content: TextField(
          controller: controller,
          maxLines: 10,
          decoration: const InputDecoration(
            hintText: 'Paste JSON data here...',
            border: OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(context, controller.text),
            child: const Text('Import'),
          ),
        ],
      ),
    );
    
    if (result != null && result.isNotEmpty) {
      try {
        await widget.bookmarkService.importBookmarks(result);
        _loadBookmarks();
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Bookmarks imported!')),
          );
        }
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Error: $e')),
          );
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Bookmarks'),
        actions: [
          IconButton(icon: const Icon(Icons.upload_file), onPressed: _exportBookmarks),
          IconButton(icon: const Icon(Icons.download), onPressed: _importBookmarks),
        ],
      ),
      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : bookmarks.isEmpty
              ? const Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.bookmark_border, size: 64, color: Colors.grey),
                      SizedBox(height: 16),
                      Text('No bookmarks yet', style: TextStyle(fontSize: 18, color: Colors.grey)),
                    ],
                  ),
                )
              : ListView.builder(
                  itemCount: bookmarks.length,
                  itemBuilder: (context, index) {
                    final bookmark = bookmarks[index];
                    return Card(
                      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      child: ListTile(
                        leading: const Icon(Icons.bookmark, color: Colors.orange),
                        title: Text(bookmark.title, maxLines: 1, overflow: TextOverflow.ellipsis),
                        subtitle: Text(bookmark.url, maxLines: 1, overflow: TextOverflow.ellipsis,
                          style: const TextStyle(fontSize: 12)),
                        trailing: IconButton(
                          icon: const Icon(Icons.delete, color: Colors.red),
                          onPressed: () => _deleteBookmark(bookmark.id),
                        ),
                        onTap: () {
                          widget.onBookmarkTap(bookmark.url);
                          Navigator.pop(context);
                        },
                      ),
                    );
                  },
                ),
    );
  }
}