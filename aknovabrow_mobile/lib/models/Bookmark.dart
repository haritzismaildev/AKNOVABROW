class Bookmark {
  final String id;
  final String title;
  final String url;
  final DateTime createdAt;

  Bookmark({
    required this.id,
    required this.title,
    required this.url,
    required this.createdAt,
  });

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'url': url,
        'createdAt': createdAt.toIso8601String(),
      };

  factory Bookmark.fromJson(Map<String, dynamic> json) => Bookmark(
        id: json['id'],
        title: json['title'],
        url: json['url'],
        createdAt: DateTime.parse(json['createdAt']),
      );
}