using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.IO;

namespace MWBookmark;

public class BookmarkItem : INotifyPropertyChanged, IDisposable
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Name { get; set; } = "";
    public ReactiveProperty<string> Comment { get; set; } = new("");
    public string Category { get; set; } = "";
    public bool IsDir { get; set; }
    public string DispComment
    {
        get
        {
            if (Comment.Value is null) return "";
            return Comment.Value.Replace("\r", "").Replace("\n", " ") ?? "";
        }
    }
    public BookmarkItem() {}
    public BookmarkItem(BookmarkEntity item)
    {
        Id = item.Id;
        FullName = item.FullName;
        Name = item.Name;
        Comment.Value = item.Comment;
        Category = item.Category;
        IsDir = item.IsDir;
        
        Comment.Subscribe(x =>
        {
            OnPropertyChanged(nameof(DispComment));
        })
        .AddTo(Disposable);
    }

    #region INotifyPropertyChanged, IDisposable
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected CompositeDisposable Disposable { get; } = [];
	public void Dispose() => Disposable.Dispose();
    #endregion    
}