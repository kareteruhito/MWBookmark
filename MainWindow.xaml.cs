using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MWBookmark;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    public MainWindow()
    {
        InitializeComponent();
        
        var vm = new MainWindowViewModel();

        this.DataContext = vm;

        this.Closed += (_, _) => Window_Closed();
    }

    private void Window_Closed()
    {
        if (this.DataContext is IDisposable disposable)
            disposable.Dispose();
    }

    private void ItemList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemList.SelectedItem is not BookmarkItem item) return;
        if (!File.Exists(item.FullName) && !Directory.Exists(item.FullName)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = item.FullName,
            UseShellExecute = true
        });
    }

    private void ItemList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void ItemList_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPosition = e.GetPosition(null);

        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem is null) return;

        if (listViewItem.DataContext is not BookmarkItem item) return;
        if (!File.Exists(item.FullName) && !Directory.Exists(item.FullName)) return;

        var data = new DataObject();
        data.SetData(DataFormats.FileDrop, new[] { item.FullName });

        DragDrop.DoDragDrop(listViewItem, data, DragDropEffects.Copy);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T target) return target;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}


// mkdir .\Publish
// dotnet build .\MWBookmark.csproj -c Release -o .\Publish