using System.Windows;

namespace SeniorUtilities;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        rbRu.IsChecked = App.CurrentLanguage == "ru";
        rbEn.IsChecked = App.CurrentLanguage == "en";
        cbAutoStart.IsChecked = AutoStart.IsEnabled;
        cbTray.IsChecked = Settings.Current.MinimizeToTray;

        cbOcrLang.SelectedIndex = Settings.Current.OcrLanguage switch
        {
            "ru" => 1,
            "en" => 2,
            _    => 0,
        };
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        App.SetLanguage(rbEn.IsChecked == true ? "en" : "ru");

        Settings.Current.MinimizeToTray = cbTray.IsChecked == true;
        Settings.Current.OcrLanguage = cbOcrLang.SelectedIndex switch
        {
            1 => "ru",
            2 => "en",
            _ => "auto",
        };
        Settings.Save();

        AutoStart.Set(cbAutoStart.IsChecked == true);

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
