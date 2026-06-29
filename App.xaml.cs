using Application = System.Windows.Application;
using System.Windows;

namespace SeniorUtilities;

public partial class App : Application
{
    public static string CurrentLanguage { get; private set; } = "ru";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings.Load();
        ApplyLanguage(Settings.Current.Language is "en" or "ru" ? Settings.Current.Language : "ru");
    }

    public static void SetLanguage(string lang)
    {
        ApplyLanguage(lang);
        Settings.Current.Language = lang;
        Settings.Save();
    }

    public static void ApplyLanguage(string lang)
    {
        CurrentLanguage = lang;
        var dict = new ResourceDictionary
        {
            Source = new Uri($"Resources/Strings.{lang}.xaml", UriKind.Relative)
        };
        var merged = Current.Resources.MergedDictionaries;
        var old = merged.FirstOrDefault(d =>
            d.Source?.OriginalString?.StartsWith("Resources/Strings.") == true);
        if (old is not null) merged.Remove(old);
        merged.Add(dict);
    }

    public static string Res(string key) => (string)Current.Resources[key];
}
