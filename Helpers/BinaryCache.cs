using LiteDB;
using System.IO;

namespace Maywork.WPF.Helpers;

public sealed class BinaryCache : IDisposable
{
    private readonly LiteDatabase _db;

    public BinaryCache(string dbPath)
    {
        _db = new LiteDatabase(dbPath);
    }

    public void Save(string key, Stream stream, string fileName = "cache.bin")
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(nameof(key));

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // 例: key = "thumbnail/sample1"
        _db.FileStorage.Upload(key, fileName, stream);
    }

    public void Save(string key, byte[] bytes, string fileName = "cache.bin")
    {
        using var ms = new MemoryStream(bytes);
        Save(key, ms, fileName);
    }

    public bool Exists(string key)
    {
        return _db.FileStorage.Exists(key);
    }

    public byte[]? LoadBytes(string key)
    {
        if (!_db.FileStorage.Exists(key))
            return null;

        using var ms = new MemoryStream();
        _db.FileStorage.Download(key, ms);
        return ms.ToArray();
    }

    public bool LoadToStream(string key, Stream output)
    {
        if (!_db.FileStorage.Exists(key))
            return false;

        _db.FileStorage.Download(key, output);
        return true;
    }

    public bool Delete(string key)
    {
        return _db.FileStorage.Delete(key);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
/*
// LiteDBの導入
dotnet add package LiteDB



// 使用例
using var cache = new BinaryCache("image-cache.db");

// 保存
using (var fs = File.OpenRead(@"C:\temp\sample.jpg"))
{
    cache.Save("image/sample.jpg", fs, "sample.jpg");
}

// 読み込み
byte[]? bytes = cache.LoadBytes("image/sample.jpg");

if (bytes != null)
{
    File.WriteAllBytes(@"C:\temp\restored.jpg", bytes);
}

*/