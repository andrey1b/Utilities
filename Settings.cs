using System.IO;
using System.Text.Json;

namespace SeniorUtilities;

/// <summary>
/// Простое хранилище настроек в %LOCALAPPDATA%\SeniorUtilities\settings.json.
/// Без внешних зависимостей — одна программа, один файл.
/// </summary>
public sealed class Settings
{
    public string Language { get; set; } = "ru";

    /// <summary>Свернуть в трей при закрытии окна (фоновая работа горячих клавиш).</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>Включена ли утилита исправления раскладки (горячая клавиша).</summary>
    public bool LayoutFixEnabled { get; set; } = true;

    // ── хранение ─────────────────────────────────────────────────────────────

    public static Settings Current { get; private set; } = new();

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SeniorUtilities");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath));
                if (loaded is not null) Current = loaded;
            }
        }
        catch { /* битый файл — используем значения по умолчанию */ }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* нет прав на запись — не критично */ }
    }
}
