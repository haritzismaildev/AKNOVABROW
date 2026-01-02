using System;

namespace AKNOVABROW.Models
{
    public class Bookmark
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Folder { get; set; } = "Default";
    }
}