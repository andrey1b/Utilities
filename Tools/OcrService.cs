using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace SeniorUtilities.Tools;

/// <summary>
/// Распознавание текста на изображении встроенным движком Windows
/// (Windows.Media.Ocr) — без внешних зависимостей. Поддерживает RU и EN,
/// если установлены соответствующие языковые компоненты Windows.
/// </summary>
public static class OcrService
{
    /// <summary>Установлен ли хоть один движок распознавания.</summary>
    public static bool IsAvailable => OcrEngine.AvailableRecognizerLanguages.Count > 0;

    /// <summary>
    /// Создаёт движок по коду языка: "auto" — из языков системы,
    /// "ru" / "en" — конкретный. С откатом на любой доступный.
    /// </summary>
    private static OcrEngine? CreateEngine(string lang)
    {
        OcrEngine? engine = lang switch
        {
            "ru" => OcrEngine.TryCreateFromLanguage(new Language("ru")),
            "en" => OcrEngine.TryCreateFromLanguage(new Language("en-US")),
            _    => OcrEngine.TryCreateFromUserProfileLanguages(),
        };
        engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            var first = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault();
            if (first is not null) engine = OcrEngine.TryCreateFromLanguage(first);
        }
        return engine;
    }

    /// <summary>Распознаёт текст на изображении. Возвращает строки, разделённые \n.</summary>
    public static async Task<string> RecognizeAsync(Bitmap image, string lang)
    {
        var engine = CreateEngine(lang);
        if (engine is null) return "";

        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Bmp);
        ms.Position = 0;

        var decoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
        using var software = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        var result = await engine.RecognizeAsync(software);
        return string.Join("\n", result.Lines.Select(l => l.Text));
    }
}
