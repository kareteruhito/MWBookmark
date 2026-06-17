// アプリケーション固有の保存パスを一元管理する Helper。
using System.Reflection;

namespace Maywork.WPF.Helpers;

public static class AppPathHelper
{
    /// アプリケーション名。
    public static string AppName { get; } = GetDefaultAppName();

    // ------------------------------------------------------------
    // static ctor
    // ------------------------------------------------------------

    static AppPathHelper()
    {
        EnsureDirectories();
    }

    // ------------------------------------------------------------
    // public paths
    // ------------------------------------------------------------

    /// 設定ファイル・ユーザー設定用（APPDATA）
    public static string Roaming =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            AppName);

    /// キャッシュ・ログ・一時データ用（LOCALAPPDATA）
    public static string Local =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            AppName);

    public static string SettingsFile =>
        System.IO.Path.Combine(Roaming, "settings.json");

    public static string CacheDir =>
        System.IO.Path.Combine(Local, "cache");

    public static string LogDir =>
        System.IO.Path.Combine(Local, "log");

    public static string TempDir =>
        System.IO.Path.Combine(Local, "temp");

    // ------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------

    private static string GetDefaultAppName()
    {
        var asm = Assembly.GetEntryAssembly();
        return asm?.GetName().Name ?? "Application";
    }

    /// <summary>
    /// 必要なディレクトリをすべて作成する。
    /// </summary>
    public static void EnsureDirectories()
    {
        System.IO.Directory.CreateDirectory(Roaming);
        System.IO.Directory.CreateDirectory(Local);
        System.IO.Directory.CreateDirectory(CacheDir);
        System.IO.Directory.CreateDirectory(LogDir);
        System.IO.Directory.CreateDirectory(TempDir);
    }
}

/*

 【APPDATA (Roaming)】
 ・ユーザー設定ファイル
 ・UIレイアウト
 ・履歴情報
 ・ユーザー辞書など

 ※PC移行・ドメイン環境ではローミング対象。


 【LOCALAPPDATA (Local)】
 ・キャッシュデータ
 ・ログファイル
 ・一時ファイル
 ・画像サムネイル

 ※削除されても再生成可能なデータを保存する。
*/