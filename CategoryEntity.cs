using LiteDB;

namespace MWBookmark;

public class CategoryEntity
{
    public const string DefaultName = "未分類";
    
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}
