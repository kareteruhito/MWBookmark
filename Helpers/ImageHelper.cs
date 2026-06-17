using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Maywork.WPF.Helpers;

public static partial class ImageHelper
{
    // 画像ファイルを読み込むメソッド
    public static BitmapSource Load(Stream stream)
    {
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        var frame = decoder.Frames[0];
        frame.Freeze();

        return frame;
    }
    public static BitmapSource Load(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var bmp =  Load(stream);
        
        if (bmp.CanFreeze && !bmp.IsFrozen)
            bmp.Freeze();

        return bmp;
    }

    // 画像の幅・高さを取得するメソッド
    public static (int width, int height) GetSize(Stream stream)
    {
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.DelayCreation,
            BitmapCacheOption.None);

        var frame = decoder.Frames[0];

        return(frame.PixelWidth, frame.PixelHeight);
    }
    public static (int width, int height) GetSize(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return GetSize(stream);
    }

    // 96DPIに変換する
    public static BitmapSource To96Dpi(BitmapSource source)
    {
        if (Math.Abs(source.DpiX - 96) < 0.01 &&
            Math.Abs(source.DpiY - 96) < 0.01)
        {
            return ConvertToBgra32(source);
        }

        var rtb = new RenderTargetBitmap(
            source.PixelWidth,
            source.PixelHeight,
            96,
            96,
            PixelFormats.Pbgra32);   // ← ここはPbgra32固定

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(source,
                new Rect(0, 0, source.PixelWidth, source.PixelHeight));
        }

        rtb.Render(dv);
        rtb.Freeze();

        return ConvertToBgra32(rtb);   // ← 後からBgra32へ変換
    }
    private static BitmapSource ConvertToBgra32(BitmapSource source)
    {
        if (source.Format == PixelFormats.Bgra32)
        {
            if (source.CanFreeze && !source.IsFrozen)
                source.Freeze();
            return source;
        }

        var converted = new FormatConvertedBitmap(
            source,
            PixelFormats.Bgra32,
            null,
            0);

        converted.Freeze();
        return converted;
    }

    // Imageコントロール対応拡張子判定
    private static readonly HashSet<string> _supportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".gif",
            ".tif",
            ".tiff",
            ".webp"
        };

    /// <summary>
    /// 画像として扱う拡張子か判定する
    /// </summary>
    public static bool IsSupportedImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var ext = Path.GetExtension(path);

        if (string.IsNullOrEmpty(ext))
            return false;

        return _supportedExtensions.Contains(ext);
    }
    // ヒストグラム生成
    public static BitmapSource CreateHistogram(BitmapSource source)
    {
		int width = source.PixelWidth;
		int height = source.PixelHeight;

		int stride = width * 4;
		byte[] pixels = new byte[stride * height];
		source.CopyPixels(pixels, stride, 0);

		int[] hist = new int[256];

		for (int i = 0; i < pixels.Length; i += 4)
		{
			byte b = pixels[i];
			byte g = pixels[i + 1];
			byte r = pixels[i + 2];

			// Rec.709 輝度
			int y = (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);

			hist[y]++;
		}

		int max = hist.Max();

		int histHeight = 200;
		int histWidth = 512;
		int barWidth = 2;

		var wb = new WriteableBitmap(
			histWidth, histHeight, 96, 96,
			PixelFormats.Bgra32, null);

		byte[] histPixels = new byte[histWidth * histHeight * 4];

		// 背景黒
		for (int i = 0; i < histPixels.Length; i += 4)
			histPixels[i + 3] = 255;

		for (int level = 0; level < 256; level++)
		{
			int xStart = level * barWidth;

			int value = hist[level] * histHeight / max;

			for (int y = 0; y < value; y++)
			{
				for (int w = 0; w < barWidth; w++)
				{
					int index =
						((histHeight - 1 - y) * histWidth + xStart + w) * 4;

					// 白で描画
					histPixels[index + 0] = 255; // B
					histPixels[index + 1] = 255; // G
					histPixels[index + 2] = 255; // R
					histPixels[index + 3] = 255;
				}
			}
		}

		wb.WritePixels(
			new Int32Rect(0, 0, histWidth, histHeight),
			histPixels,
			histWidth * 4,
			0);

		return wb;
    }

    /// <summary>
    /// BitmapSource を PNG 形式で保存する
    /// </summary>
    public static void SavePng(BitmapSource source, string path)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        encoder.Save(stream);
    }

    /// <summary>
    /// 指定サイズ以内に収まるよう縮小する
    /// </summary>
    /// <param name="source">元画像</param>
    /// <param name="maxWidth">最大幅</param>
    /// <param name="maxHeight">最大高さ</param>
    /// <returns>縮小後画像</returns>
    public static BitmapSource CreateThumbnail(
        BitmapSource source,
        int maxWidth,
        int maxHeight)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        double scaleX = (double)maxWidth / source.PixelWidth;
        double scaleY = (double)maxHeight / source.PixelHeight;

        double scale = Math.Min(scaleX, scaleY);

        // 元画像の方が小さい場合はそのまま返す
        if (scale >= 1.0)
            return source;

        var transformed = new TransformedBitmap(
            source,
            new ScaleTransform(scale, scale));

        transformed.Freeze();

        return transformed;
    }

    /// <summary>
    /// JPEG形式で保存する
    /// </summary>
    public static void SaveJpeg(
        BitmapSource bitmap,
        string fileName,
        int quality = 90)
    {
        using (var stream = File.Create(fileName))
        {
            SaveJpeg(bitmap, stream, quality);
        }
    }

    /// <summary>
    /// JPEG形式で保存する
    /// </summary>
    public static void SaveJpeg(
        BitmapSource bitmap,
        Stream stream,
        int quality = 90)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        quality = Math.Max(1, Math.Min(100, quality));

        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = quality
        };

        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
    }
}

/*
 // 使用例

LoadCommand = new RelayCommand(async _ =>
{
    var path = @"C:\temp\test.jpg";

    if (!ImageHelper.IsSupportedImage(path))
        return;

    var bmp = await Task.Run(() =>
    {
        var img = ImageHelper.Load(path);
        return ImageHelper.To96Dpi(img);
    });

    Image = bmp;
});
 */