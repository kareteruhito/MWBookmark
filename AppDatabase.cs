using LiteDB;

namespace MWBookmark;

public class AppDatabase : IDisposable
{
    private readonly LiteDatabase _db;

    public ILiteCollection<BookmarkEntity> Bookmarks
        => _db.GetCollection<BookmarkEntity>("bookmarks");
    public ILiteCollection<CategoryEntity> Categories
        => _db.GetCollection<CategoryEntity>("categories");

    public AppDatabase()
    {
        _db = new LiteDatabase("Bookmarks.db");

        Bookmarks.EnsureIndex(x => x.Comment, unique: false);
        Categories.EnsureIndex(x => x.Name, unique: true);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}