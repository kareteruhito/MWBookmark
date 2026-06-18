using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Maywork.WPF.Helpers;
using System.IO.Compression;
using System.Windows.Media;
using System.Globalization;
using System.Windows;

namespace MWBookmark;

public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
// TODO:
    private readonly AppDatabase _db = new();
 
    public ReactiveCollection<BookmarkItem> Bookmarks { get; } = [];
    public ICollectionView BookmarksView { get; }

    public ReactiveProperty<BookmarkItem?> Bookmark { get; set; } = new();
    public ReactiveCommand<string []> FileDropCommand { get; }
    public ReactiveCommand<BookmarkItem> DeleteBookmarkCommand { get; }

    public ReactiveCollection<CategoryEntity> Categories { get; } = [];
    public ReactiveProperty<CategoryEntity?> Category { get; } = new();

    public ReactiveCommand DeleteCategoryCommand { get; }
    public ReactiveCommand<string> AddCategoryCommand { get; }
    public ReactiveCommand<string> UpdateCategoryCommand { get; }
    public ReactiveProperty<BitmapSource?> ImagePreview { get; set; } = new();

    private BinaryCache _cache = new("image-cache.db");

    private BookmarkItem? _clipboardBookmark;
    private bool _isCut;

    public ReactiveCommand<BookmarkItem> CopyBookmarkCommand { get; }
    public ReactiveCommand<BookmarkItem> CutBookmarkCommand { get; }
    public ReactiveCommand PasteBookmarkCommand { get; }    
    public MainWindowViewModel()
    {
        _db.AddTo(Disposable);
        _cache.AddTo(Disposable);

        Bookmarks.AddTo(Disposable);

        BookmarksView = CollectionViewSource.GetDefaultView(Bookmarks);

        BookmarksView.Filter = x =>
        {
            if (x is not BookmarkItem item) return false;
            if (Category.Value is null) return false;

            return item.Category == Category.Value.Name;
        };

        Bookmark.Subscribe(x=>
        {
// TODO:
            ImagePreview.Value = null;
            if (x is null) return;
            
            
            using MemoryStream stream = new();

            // *** 画像ファイル ***
            if (ImageHelper.IsSupportedImage(x.FullName))
            {
                if (_cache.LoadToStream(x.FullName, stream))
                {
var sw = Stopwatch.StartNew();
                    stream.Position = 0;
                    ImagePreview.Value = ImageHelper.Load(stream);
sw.Stop();
Debug.Print($"IMG Cache OK {sw.ElapsedMilliseconds}ms");
                }
                else
                {
var sw = Stopwatch.StartNew();
                    var source = ImageHelper.Load(x.FullName);
                    var thumb = ImageHelper.CreateThumbnail(source, 512, 512);
                    var bmp = ImageHelper.To96Dpi(thumb);
                    ImageHelper.SaveJpeg(bmp, stream);
                    byte[] buff = stream.ToArray();
                    _cache.Save(x.FullName, buff, x.Name);

                    ImagePreview.Value = bmp;
sw.Stop();
Debug.Print($"IMG Cache NG {sw.ElapsedMilliseconds}ms");
                }
                return;
            }
        
            // *** ディレクトリ ***
            if (x.IsDir)
            {
                var imgPath = Directory.GetFiles(x.FullName)
                    .Where(x => ImageHelper.IsSupportedImage(x))
                    .FirstOrDefault();
                
                if (imgPath is not null)
                {
                    Debug.Print($"{imgPath}");

                    if (_cache.LoadToStream(imgPath, stream))
                    {
var sw = Stopwatch.StartNew();
                        stream.Position = 0;
                        ImagePreview.Value = ImageHelper.Load(stream);
sw.Stop();
Debug.Print($"DIR Cache OK {sw.ElapsedMilliseconds}ms");
                    }
                    else
                    {
var sw = Stopwatch.StartNew();
                        var source = ImageHelper.Load(imgPath);
                        var thumb = ImageHelper.CreateThumbnail(source, 512, 512);
                        var bmp = ImageHelper.To96Dpi(thumb);
                        ImageHelper.SaveJpeg(bmp, stream);
                        byte[] buff = stream.ToArray();
                        _cache.Save(imgPath, buff, x.Name);

                        ImagePreview.Value = bmp;
sw.Stop();
Debug.Print($"DIR Cache NG {sw.ElapsedMilliseconds}ms");
                    }
                }
                else
                {
                    ImagePreview.Value = TextToBitmapSource("📁");
                }
                return;
            }

            string ext = Path.GetExtension(x.FullName).ToUpper();
            if (ext == ".ZIP")
            {
                string zipPath = x.FullName;
                Debug.Print($"ZIP:{zipPath}");

                using var archive = ZipFile.OpenRead(zipPath);

                var entry = archive.Entries
                    .Where(e => ImageHelper.IsSupportedImage(e.FullName))
                    .FirstOrDefault();
                if (entry is not null)
                {
                    string key = zipPath + "/" + entry.FullName;
                    Debug.Print($"{key}");

                    if (_cache.LoadToStream(key, stream))
                    {
var sw = Stopwatch.StartNew();
                        stream.Position = 0;
                        ImagePreview.Value = ImageHelper.Load(stream);
sw.Stop();
Debug.Print($"ZIP Cache OK {sw.ElapsedMilliseconds}ms");
                    }
                    else
                    {
var sw = Stopwatch.StartNew();
                        using Stream s = entry.Open();
                        s.CopyTo(stream);
                        var source = ImageHelper.Load(stream);
                        var thumb = ImageHelper.CreateThumbnail(source, 512, 512);
                        var bmp = ImageHelper.To96Dpi(thumb);
                        ImageHelper.SaveJpeg(bmp, stream);
                        byte[] buff = stream.ToArray();
                        _cache.Save(key, buff, x.Name);

                        ImagePreview.Value = bmp;
sw.Stop();
Debug.Print($"ZIP Cache NG {sw.ElapsedMilliseconds}ms");
                    }
                }
                else
                {
                    ImagePreview.Value = TextToBitmapSource("📦");
                }
                return;
            }

            ImagePreview.Value = TextToBitmapSource("📄");
        }).AddTo(Disposable);

        FileDropCommand = new ReactiveCommand<string[]>()
            .WithSubscribe(files=>
            {
                if (Category.Value is null) return;

                foreach(var file in files)
                {
                    //Debug.Print(file);
                    var entity = new BookmarkEntity()
                    {
                        Name = Path.GetFileName(file),
                        FullName = file,
                        Comment = "",
                        Category = Category.Value.Name,
                        IsDir = Directory.Exists(file),
                    };
                    _db.Bookmarks.Insert(entity);

                    var item = new BookmarkItem(entity);
                    item.Comment
                        .Skip(1)
                        .Subscribe( x => UpdateComment())
                        .AddTo(Disposable);
                    Bookmarks.Add(item);
                }
            })
            .AddTo(Disposable);
        
        DeleteBookmarkCommand = Bookmark
            .Select(x => x is not null)
            .ToReactiveCommand<BookmarkItem>()
            .WithSubscribe(item =>
            {
                //Debug.Print(item.Name);
                if (item is null) return;
                _db.Bookmarks.Delete(item.Id);
                Bookmarks.Remove(item);
            })
            .AddTo(Disposable);
        
        LoadDB();


        // ************ カテゴリ ************
        Categories.AddTo(Disposable);

        Category.Subscribe(category =>
        {
            BookmarksView.Refresh();
        })
        .AddTo(Disposable);

        DeleteCategoryCommand = Category
            .Select(x => x is not null && x.Name != CategoryEntity.DefaultName)
            .ToReactiveCommand()
            .WithSubscribe(() =>
            {
                if (Category.Value is null) return;
                if (Category.Value.Name == CategoryEntity.DefaultName) return;

                var item = Category.Value;

                var defaultCategory = Categories
                    .First(x => x.Name == CategoryEntity.DefaultName);

                foreach (var bookmark in Bookmarks.Where(x => x.Category == item.Name))
                {
                    bookmark.Category = defaultCategory.Name;

                    var entity = ConvertToBookmarkEntity(bookmark);
                    _db.Bookmarks.Update(entity);
                }

                _db.Categories.Delete(item.Id);

                Categories.Remove(item);
                Category.Value = Categories.FirstOrDefault();
            })
            .AddTo(Disposable);

        AddCategoryCommand = new ReactiveCommand<string>()
            .WithSubscribe(name =>
            {
                name = name.Trim();

                if (string.IsNullOrWhiteSpace(name)) return;

                if (Categories.Any(x => x.Name == name)) return;

                var item = new CategoryEntity
                {
                    Name = name
                };

                _db.Categories.Insert(item);

                Categories.Add(item);
                Category.Value = item;
            })
            .AddTo(Disposable);

        UpdateCategoryCommand = new ReactiveCommand<string>()
            .WithSubscribe(name =>
            {
                if (Category.Value is null) return;
                if (Category.Value.Name == CategoryEntity.DefaultName) return;

                var item = Category.Value;

                name = name.Trim();

                if (Categories.Any(x => x.Name == name)) return;

                var newItem = new CategoryEntity
                {
                    Name = name,
                    Id = item.Id,
                };


                if (_db.Categories.Update(newItem))
                {
                    Debug.Print("成功");                    
                }

                Categories.Remove(item);
                Categories.Add(newItem);
                Category.Value = newItem;


                var array = Bookmarks
                    .Where(x => x.Category == item.Name)
                    .ToArray();
                
                foreach(var x in array)
                {
                    x.Category = newItem.Name;

                    var e = ConvertToBookmarkEntity(x);
                    if(_db.Bookmarks.Update(e))
                    {
                        Debug.Print("成功");
                    }
                }
                BookmarksView.Refresh();
            })
            .AddTo(Disposable);

        LoadCategories();

        // *** 画像プレビュー ***
        ImagePreview
            .AddTo(Disposable);
        
        // *** コンテキストメニュー ***
        CopyBookmarkCommand = new ReactiveCommand<BookmarkItem>()
            .WithSubscribe(item =>
            {
                _clipboardBookmark = item;
                _isCut = false;
            })
            .AddTo(Disposable);

        CutBookmarkCommand = new ReactiveCommand<BookmarkItem>()
            .WithSubscribe(item =>
            {
                _clipboardBookmark = item;
                _isCut = true;
            })
            .AddTo(Disposable);

        PasteBookmarkCommand = Category
            .Select(x => x is not null)
            .ToReactiveCommand()
            .WithSubscribe(() =>
            {
                if (_clipboardBookmark is null) return;
                if (Category.Value is null) return;

                if (_isCut)
                {
                    _clipboardBookmark.Category = Category.Value.Name;

                    var entity = ConvertToBookmarkEntity(_clipboardBookmark);
                    _db.Bookmarks.Update(_clipboardBookmark.Id, entity);

                    BookmarksView.Refresh();

                    _clipboardBookmark = null;
                    _isCut = false;
                }
                else
                {
                    var entity = new BookmarkEntity
                    {
                        Name = _clipboardBookmark.Name,
                        FullName = _clipboardBookmark.FullName,
                        Comment = _clipboardBookmark.Comment.Value,
                        Category = Category.Value.Name,
                        IsDir = _clipboardBookmark.IsDir,
                    };

                    _db.Bookmarks.Insert(entity);

                    var item = new BookmarkItem(entity);
                    item.Comment
                        .Skip(1)
                        .Subscribe(x => UpdateComment())
                        .AddTo(Disposable);

                    Bookmarks.Add(item);
                }
            })
            .AddTo(Disposable);
    }
// コンストラクタ ここまで

    private void LoadCategories()
    {
        var defaultCategory = _db.Categories
            .FindOne(x => x.Name == CategoryEntity.DefaultName);

        if (defaultCategory is null)
        {
            defaultCategory = new CategoryEntity
            {
                Name = CategoryEntity.DefaultName
            };

            _db.Categories.Insert(defaultCategory);
        }

        foreach (var item in _db.Categories.FindAll())
        {
            Categories.Add(item);

            if (item.Name == CategoryEntity.DefaultName)
                Category.Value = item;
        }
    }

    private void LoadDB()
    {
        foreach (var entity in _db.Bookmarks.FindAll())
        {
            var item = new BookmarkItem(entity);
            item.Comment
                .Skip(1)
                .Subscribe( x => UpdateComment())
                .AddTo(Disposable);
            Bookmarks.Add(item);
        }        
    }
    private void UpdateComment()
    {
        if (Bookmark.Value is null) return;
        var item = Bookmark.Value;
        var entity = ConvertToBookmarkEntity(item);

        _db.Bookmarks.Update(item.Id, entity);
    }
    private static BookmarkEntity ConvertToBookmarkEntity(BookmarkItem item)
    {
        return new BookmarkEntity
        {
            Id = item.Id,
            Name = item.Name,
            FullName = item.FullName,
            Comment = item.Comment.Value,
            IsDir = item.IsDir,
            Category = item.Category,
        };
    }
    public static BitmapSource TextToBitmapSource(
        string text,
        double fontSize = 128,
        string fontFamilyName = "Segoe UI Emoji")
    {
        var typeface = new Typeface(new FontFamily(fontFamilyName), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawText(formattedText, new Point(0, 0));
        }

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(formattedText.Width),
            (int)Math.Ceiling(formattedText.Height),
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }

    #region INotifyPropertyChanged, IDisposable
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected CompositeDisposable Disposable { get; } = [];
	public void Dispose() => Disposable.Dispose();
    #endregion
}