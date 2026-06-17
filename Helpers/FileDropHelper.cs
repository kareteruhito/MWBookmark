// ファイルDropをサポートするための Attached Property を提供するクラス
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Maywork.WPF.Helpers;

public static class FileDropHelper
{
    #region Enable

    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(FileDropHelper),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject obj, bool value)
        => obj.SetValue(EnableProperty, value);

    public static bool GetEnable(DependencyObject obj)
        => (bool)obj.GetValue(EnableProperty);

    #endregion

    #region Command

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached(
            "DropCommand",
            typeof(ICommand),
            typeof(FileDropHelper),
            new PropertyMetadata(null));

    public static void SetDropCommand(DependencyObject obj, ICommand value)
        => obj.SetValue(DropCommandProperty, value);

    public static ICommand GetDropCommand(DependencyObject obj)
        => (ICommand)obj.GetValue(DropCommandProperty);

    #endregion

    #region Internal Wiring

    private static void OnEnableChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        if ((bool)e.NewValue)
        {
            element.AllowDrop = true;
            element.PreviewDragOver += OnPreviewDragOver;
            element.Drop += OnDrop;
        }
        else
        {
            element.PreviewDragOver -= OnPreviewDragOver;
            element.Drop -= OnDrop;
        }
    }

    private static void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject d)
            return;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);

        var command = GetDropCommand(d);

        if (command?.CanExecute(files) == true)
        {
            command.Execute(files);
        }
    }

    #endregion

    public static void SetFileDrop(UIElement element, Action<string[]> action)
    {
        element.AllowDrop = true;

        element.PreviewDragOver += (_, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;

            e.Handled = true;
        };

        element.Drop += (_, e) =>
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (files is not null)
                action(files);
        };
    }
}
/*
// 使い方

// データバインディング
XAML内

xmlns:h="clr-namespace:Maywork.WPF.Helpers"

<Grid
    h:FileDropHelper.Enable="True"
    h:FileDropHelper.DropCommand="{Binding FileDropCommand}">
    
    <TextBlock
        Text="ここにファイルをドロップ"
        HorizontalAlignment="Center"
        VerticalAlignment="Center"/>
</Grid>

ViewModel内
public RelayCommand<string []> FileDropCommand { get; }

public MainWindowViewModel()
{
    FileDropCommand = new RelayCommand<string []>(files=>OnFileDrop(files));
}
private void OnFileDrop(string[] files)
{
    foreach (var file in files)
    {
        Debug.Print(file);
    }
}

// コードビハインド
ICommand DropCommand;

public MainWindow()
{
    DropCommand = new RelayCommand<string[]>(files => DropFile(files));

    FileDropHelper.SetDropCommand(DropBase, DropCommand);
}
// ファイルドロップ
void DropFile(string[] files)
{
    foreach(string file in files)
    {
        Debug.Print($"{file}");
    }
}
*/
/*
// コードビハインド (SetFileDrop)

public MainWindow()
{
    InitializeComponent();

    FileDropHelper.SetFileDrop(DropBase, files =>
    {
        foreach (var file in files)
        {
            Debug.WriteLine(file);
        }
    });
}
*/