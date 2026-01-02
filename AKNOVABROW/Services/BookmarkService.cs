using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AKNOVABROW.Models;

namespace AKNOVABROW.Services
{
    public class BookmarkService
    {
        private readonly string bookmarkPath;
        private List<Bookmark> bookmarks;

        public BookmarkService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "AKNOVABROW");
            Directory.CreateDirectory(folder);
            bookmarkPath = Path.Combine(folder, "bookmarks.json");
            LoadBookmarks();
        }

        public List<Bookmark> GetBookmarks() => bookmarks;

        public void AddBookmark(Bookmark bookmark)
        {
            bookmarks.Add(bookmark);
            SaveBookmarks();
        }

        public void DeleteBookmark(Bookmark bookmark)
        {
            bookmarks.Remove(bookmark);
            SaveBookmarks();
        }

        public void ExportBookmarks(string filePath)
        {
            var json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void ImportBookmarks(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var imported = JsonSerializer.Deserialize<List<Bookmark>>(json);
            if (imported != null)
            {
                bookmarks.AddRange(imported);
                SaveBookmarks();
            }
        }

        private void LoadBookmarks()
        {
            if (File.Exists(bookmarkPath))
            {
                var json = File.ReadAllText(bookmarkPath);
                bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
            }
            else
            {
                bookmarks = new List<Bookmark>();
            }
        }

        private void SaveBookmarks()
        {
            var json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(bookmarkPath, json);
        }
    }
}
