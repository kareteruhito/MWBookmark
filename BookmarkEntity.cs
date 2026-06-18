using LiteDB;

namespace MWBookmark;

public class BookmarkEntity
{
    [BsonId]
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Comment { get; set; } = "";
    public string Category { get; set; } = "";
    public bool IsDir { get; set; }
}