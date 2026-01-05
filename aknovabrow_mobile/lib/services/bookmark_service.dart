import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/bookmark.dart';

class BookmarkService {
  static const String _key = 'bookmarks';

  Future<List<Bookmark>> getBookmarks() async {
    final prefs = await SharedPreferences.getInstance();
    final String? bookmarksJson = prefs.getString(_key);
    
    if (bookmarksJson == null) return [];
    
    final List<dynamic> decoded = jsonDecode(bookmarksJson);
    return decoded.map((json) => Bookmark.fromJson(json)).toList();
  }

  Future<void> addBookmark(Bookmark bookmark) async {
    final bookmarks = await getBookmarks();
    bookmarks.add(bookmark);
    await _saveBookmarks(bookmarks);
  }

  Future<void> deleteBookmark(String id) async {
    final bookmarks = await getBookmarks();
    bookmarks.removeWhere((b) => b.id == id);
    await _saveBookmarks(bookmarks);
  }

  Future<void> _saveBookmarks(List<Bookmark> bookmarks) async {
    final prefs = await SharedPreferences.getInstance();
    final String encoded = jsonEncode(bookmarks.map((b) => b.toJson()).toList());
    await prefs.setString(_key, encoded);
  }

  Future<String> exportBookmarks() async {
    final bookmarks = await getBookmarks();
    return jsonEncode(bookmarks.map((b) => b.toJson()).toList());
  }

  Future<void> importBookmarks(String jsonString) async {
    final List<dynamic> decoded = jsonDecode(jsonString);
    final imported = decoded.map((json) => Bookmark.fromJson(json)).toList();
    final existing = await getBookmarks();
    existing.addAll(imported);
    await _saveBookmarks(existing);
  }
}