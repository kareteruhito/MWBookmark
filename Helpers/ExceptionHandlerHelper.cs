// アプリケーション例外を捕まえる例外ハンドラ
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Maywork.WPF.Helpers;

public static class ExceptionHandlerHelper
{
    // ===== 振る舞い差し替え用 =====

    /// <summary>
    /// 例外ログ出力処理
    /// (category, exception)
    /// </summary>
    public static Action<string, Exception?> LogAction { get; set; }
        = DefaultLogAction;

    /// <summary>
    /// true = アプリ継続
    /// false = アプリ終了
    /// </summary>
    public static bool HandleAndContinue { get; set; } = false;

    // ===== 初期化 =====

    public static void RegisterGlobalHandlers(Application app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    // ===== 各種ハンドラ =====

    private static void OnDispatcherUnhandledException(
        object? sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        LogAction("UI Thread", e.Exception);
        e.Handled = HandleAndContinue;
    }

    private static void OnDomainUnhandledException(
        object? sender,
        UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        LogAction("AppDomain", ex);
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        LogAction("TaskScheduler", e.Exception);
        e.SetObserved();
    }

    // ===== デフォルト動作 =====

    private static void DefaultLogAction(string category, Exception? ex)
    {
        Debug.WriteLine($"[{category}] {ex}");
    }
}
/*
// 使用例

App.xaml.cs
class App : 

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    ExceptionHandlerHelper.LogAction = (category, ex) =>
    {
        // 好きなログ処理へ差し替え可能
        File.AppendAllText(
            "error.log",
            $"[{DateTime.Now}] [{category}] {ex}\n");
    };

    ExceptionHandlerHelper.HandleAndContinue = false;

    ExceptionHandlerHelper.RegisterGlobalHandlers(this);
}

又は

public partial class App : Application
{
    public App()
    {
        ExceptionHandlerHelper.RegisterGlobalHandlers(this); // これだけだとデフォルト動作
    }
}
 */