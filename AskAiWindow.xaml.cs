using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WMedia = System.Windows.Media;

namespace SeniorUtilities;

// Окно «Спросить у ИИ» — единый узнаваемый элемент SeniorHub (самолокализующееся, тёмная тема).
public partial class AskAiWindow : Window
{
    // Язык интерфейса задаётся хостом перед открытием ("ru" | "en" | "uk"); по умолчанию русский.
    public static string UiLang { get; set; } = "ru";

    private static readonly (string Name, string Url, string ApiId)[] AiList =
    {
        ("ChatGPT",    "https://chat.openai.com",         ""),
        ("Claude",     "https://claude.ai",                "claude"),
        ("Gemini",     "https://gemini.google.com",        "gemini"),
        ("Copilot",    "https://copilot.microsoft.com",    ""),
        ("Perplexity", "https://www.perplexity.ai",        "perplexity"),
        ("DeepSeek",   "https://chat.deepseek.com",        "deepseek"),
    };

    private static readonly (byte r, byte g, byte b)[] AiColors =
    {
        (16,  163, 127), (190,  90,  40), (66,  133, 244),
        (0,   120, 212), (20,  100, 180), (50,   80, 200),
    };

    // Встроенная локализация: ключ → (ru, en, uk)
    private static readonly Dictionary<string, (string ru, string en, string uk)> L = new()
    {
        ["title"]      = ("Спросить у ИИ", "Ask AI", "Запитати ШІ"),
        ["question"]   = ("Вопрос:", "Question:", "Питання:"),
        ["ask"]        = ("▶  Спросить", "▶  Ask", "▶  Запитати"),
        ["save_all"]   = ("Сохранить все", "Save all", "Зберегти все"),
        ["clear"]      = ("Очистить", "Clear", "Очистити"),
        ["api_keys"]   = ("⚙ API ключи", "⚙ API keys", "⚙ API ключі"),
        ["save"]       = ("Сохранить", "Save", "Зберегти"),
        ["copy"]       = ("Копировать", "Copy", "Копіювати"),
        ["copied"]     = ("Скопировано в буфер", "Copied to clipboard", "Скопійовано в буфер"),
        ["quick"]      = ("Быстрые вопросы:", "Quick questions:", "Швидкі питання:"),
        ["qb1"]        = ("Раскладка", "Keyboard layout", "Розкладка"),
        ["qb2"]        = ("Текст с экрана", "Text from screen", "Текст з екрана"),
        ["qb3"]        = ("Сон и гибернация", "Sleep & hibernate", "Сон і гібернація"),
        ["q1"]         = ("Как настроить и быстро переключать раскладку клавиатуры в Windows?",
                          "How to set up and quickly switch the keyboard layout in Windows?",
                          "Як налаштувати і швидко перемикати розкладку клавіатури у Windows?"),
        ["q2"]         = ("Как распознать текст с изображения или снимка экрана (OCR)? Подскажи способы.",
                          "How to recognize text from an image or screenshot (OCR)? Suggest ways.",
                          "Як розпізнати текст із зображення або знімка екрана (OCR)? Підкажи способи."),
        ["q3"]         = ("Чем отличаются спящий режим, гибернация и гибридный сон в Windows?",
                          "What is the difference between sleep, hibernation and hybrid sleep in Windows?",
                          "Чим відрізняються сплячий режим, гібернація і гібридний сон у Windows?"),
        ["empty_title"]= ("Вопрос пуст", "Empty question", "Питання порожнє"),
        ["empty_msg"]  = ("Введите вопрос.", "Enter a question.", "Введіть питання."),
        ["nothing_msg"]= ("Нет ответа для сохранения.", "No answer to save.", "Немає відповіді для збереження."),
        ["wait"]       = ("⌛ Жду ответы от ИИ…", "⌛ Waiting for AI…", "⌛ Чекаю відповіді ШІ…"),
        ["done"]       = ("✓ Готово!", "✓ Done!", "✓ Готово!"),
        ["browser"]    = ("🌐 Открыт(ы) в браузере: {0}", "🌐 Opened in browser: {0}", "🌐 Відкрито в браузері: {0}"),
        ["none"]       = ("Нет выбранных ИИ.", "No AI selected.", "Немає вибраних ШІ."),
        ["browser_note"]=("🌐 Вопрос открыт в браузере.\nВопрос скопирован в буфер — вставьте (Ctrl+V) в чат.\nСкопируйте ответ сюда после получения.",
                          "🌐 Question opened in the browser.\nIt is copied to the clipboard — paste (Ctrl+V) into the chat.\nCopy the answer back here.",
                          "🌐 Питання відкрито в браузері.\nПитання скопійовано в буфер — вставте (Ctrl+V) у чат.\nСкопіюйте відповідь сюди."),
        ["req_to"]     = ("⌛ Запрос к {0}…", "⌛ Request to {0}…", "⌛ Запит до {0}…"),
        ["err"]        = ("❌ Ошибка", "❌ Error", "❌ Помилка"),
        ["stats"]      = ("Символов: {0} | Слов: {1}", "Chars: {0} | Words: {1}", "Символів: {0} | Слів: {1}"),
        ["save_header"]= ("Ответы ИИ", "AI answers", "Відповіді ШІ"),
        ["keys_title"] = ("API ключи для ИИ", "API keys for AI", "API ключі для ШІ"),
        ["keys_save"]  = ("Сохранить", "Save", "Зберегти"),
        ["k_claude"]   = ("Получить на console.anthropic.com", "Get it at console.anthropic.com", "Отримати на console.anthropic.com"),
        ["k_gemini"]   = ("Бесплатно на aistudio.google.com", "Free at aistudio.google.com", "Безкоштовно на aistudio.google.com"),
        ["k_deepseek"] = ("Получить на platform.deepseek.com", "Get it at platform.deepseek.com", "Отримати на platform.deepseek.com"),
        ["k_perplexity"]=("Получить на perplexity.ai/settings/api", "Get it at perplexity.ai/settings/api", "Отримати на perplexity.ai/settings/api"),
    };

    private static string S(string key)
    {
        if (!L.TryGetValue(key, out var t)) return key;
        return UiLang switch { "en" => t.en, "uk" => t.uk, _ => t.ru };
    }

    private readonly ObservableCollection<AiRow> aiRows = new();

    private string _claudeApiKey = "", _geminiApiKey = "", _deepSeekApiKey = "", _perplexityApiKey = "";

    private static readonly WMedia.Brush BrushAnswer = Frozen(230, 230, 230);
    private static readonly WMedia.Brush BrushError  = Frozen(255, 110, 110);
    private static readonly WMedia.Brush BrushDim    = Frozen(154, 167, 180);

    private static WMedia.Brush Frozen(byte r, byte g, byte b)
    {
        var br = new WMedia.SolidColorBrush(WMedia.Color.FromRgb(r, g, b));
        br.Freeze();
        return br;
    }

    private static string AiDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Utilities");

    public AskAiWindow()
    {
        InitializeComponent();

        for (int i = 0; i < AiList.Length; i++)
        {
            var (name, url, apiId) = AiList[i];
            aiRows.Add(new AiRow
            {
                Name = name, Url = url, ApiId = apiId,
                HeaderBrush = Frozen(AiColors[i].r, AiColors[i].g, AiColors[i].b),
                StatsFormat = (c, w) => string.Format(S("stats"), c.ToString("N0"), w.ToString("N0"))
            });
        }
        icAiRows.ItemsSource = aiRows;

        btnAiAsk.Click     += async (_, _) => await AskAllAisAsync();
        btnAiSaveAll.Click += (_, _) => SaveAllResponses();
        btnAiClear.Click   += (_, _) => ClearAllResponses();
        btnAiApiKeys.Click += (_, _) => ShowApiKeyDialog();
        txAiQuestion.KeyDown += async (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) { e.Handled = true; await AskAllAisAsync(); }
        };
        btnAiQuick1.Click += (_, _) => txAiQuestion.Text = S("q1");
        btnAiQuick2.Click += (_, _) => txAiQuestion.Text = S("q2");
        btnAiQuick3.Click += (_, _) => txAiQuestion.Text = S("q3");

        LoadAiSettings();
        ApplyLoc();
    }

    private void ApplyLoc()
    {
        Title                = S("title");
        TbAiQuestionLbl.Text = S("question");
        btnAiAsk.Content     = S("ask");
        btnAiSaveAll.Content = S("save_all");
        btnAiClear.Content   = S("clear");
        btnAiApiKeys.Content = S("api_keys");
        TbAiQuickLbl.Text    = S("quick");
        btnAiQuick1.Content  = S("qb1");
        btnAiQuick2.Content  = S("qb2");
        btnAiQuick3.Content  = S("qb3");
        foreach (var r in aiRows)
        {
            r.SaveLabel = "💾 " + S("save");
            r.CopyLabel = "📋 " + S("copy");
        }
    }

    private void AiOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is AiRow r)
            Process.Start(new ProcessStartInfo { FileName = r.Url, UseShellExecute = true });
    }

    private void AiSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is AiRow r) SaveSingleResponse(r);
    }

    private void AiCopyButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not AiRow r) return;
        string txt = r.Response.Trim();
        if (string.IsNullOrEmpty(txt))
        { MessageBox.Show(S("nothing_msg"), S("empty_title"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Clipboard.SetText(txt);
        lblAiStatus.Text = "📋 " + S("copied") + " (" + r.Name + ")";
    }

    private void ClearAllResponses()
    {
        foreach (var r in aiRows) { r.Response = ""; r.ResponseBrush = BrushAnswer; }
        lblAiStatus.Text = "";
    }

    private async Task AskAllAisAsync()
    {
        string question = txAiQuestion.Text.Trim();
        if (string.IsNullOrEmpty(question))
        { MessageBox.Show(S("empty_msg"), S("empty_title"), MessageBoxButton.OK, MessageBoxImage.Information); return; }

        Clipboard.SetText(question);

        var tasks = new List<Task>();
        var browserOpened = new List<string>();

        for (int i = 0; i < aiRows.Count; i++)
        {
            var r = aiRows[i];
            if (!r.Enabled) continue;

            int idx = i;
            string? apiKey = ApiKeyFor(r.ApiId);
            bool hasKey = !string.IsNullOrEmpty(apiKey);

            if (r.ApiId == "claude" && hasKey)
            { r.ResponseBrush = BrushAnswer; r.Response = string.Format(S("req_to"), r.Name); tasks.Add(AskClaudeAsync(idx, question, apiKey!)); }
            else if (r.ApiId == "gemini" && hasKey)
            { r.ResponseBrush = BrushAnswer; r.Response = string.Format(S("req_to"), r.Name); tasks.Add(AskGeminiAsync(idx, question, apiKey!)); }
            else if (r.ApiId == "deepseek" && hasKey)
            { r.ResponseBrush = BrushAnswer; r.Response = string.Format(S("req_to"), r.Name); tasks.Add(AskDeepSeekAsync(idx, question, apiKey!)); }
            else if (r.ApiId == "perplexity" && hasKey)
            { r.ResponseBrush = BrushAnswer; r.Response = string.Format(S("req_to"), r.Name); tasks.Add(AskPerplexityAsync(idx, question, apiKey!)); }
            else
            {
                string openUrl = BuildBrowserUrl(r.Name, r.Url, question);
                Process.Start(new ProcessStartInfo { FileName = openUrl, UseShellExecute = true });
                r.ResponseBrush = BrushAnswer;
                r.Response = S("browser_note");
                browserOpened.Add(r.Name);
            }
        }

        if (tasks.Count > 0)
        {
            lblAiStatus.Text = S("wait");
            await Task.WhenAll(tasks);
            lblAiStatus.Text = S("done");
        }
        else
        {
            lblAiStatus.Text = browserOpened.Count > 0
                ? string.Format(S("browser"), string.Join(", ", browserOpened))
                : S("none");
        }
    }

    private string? ApiKeyFor(string apiId) => apiId switch
    {
        "claude"     => string.IsNullOrEmpty(_claudeApiKey)     ? null : _claudeApiKey,
        "gemini"     => string.IsNullOrEmpty(_geminiApiKey)     ? null : _geminiApiKey,
        "deepseek"   => string.IsNullOrEmpty(_deepSeekApiKey)   ? null : _deepSeekApiKey,
        "perplexity" => string.IsNullOrEmpty(_perplexityApiKey) ? null : _perplexityApiKey,
        _            => null
    };

    private static string BuildBrowserUrl(string name, string url, string question)
    {
        string q = Uri.EscapeDataString(question);
        return name switch
        {
            "Perplexity" => $"https://www.perplexity.ai/search?q={q}",
            "Copilot"    => $"https://www.bing.com/search?q={q}&showconv=1",
            _            => url
        };
    }

    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    private async Task AskClaudeAsync(int idx, string question, string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", key);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = "claude-sonnet-4-6", max_tokens = 1024,
                messages = new[] { new { role = "user", content = question } }
            }), Encoding.UTF8, "application/json");
            var resp = await _httpClient.SendAsync(req);
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { SetResponse(idx, $"{S("err")} Claude ({(int)resp.StatusCode}): {json}", BrushError); return; }
            using var doc = JsonDocument.Parse(json);
            SetResponse(idx, doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "", BrushAnswer);
        }
        catch (Exception ex) { SetResponse(idx, $"{S("err")}: {ex.Message}", BrushError); }
    }

    private async Task AskGeminiAsync(int idx, string question, string key)
    {
        try
        {
            string url  = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={key}";
            string body = $"{{\"contents\":[{{\"parts\":[{{\"text\":{JsonSerializer.Serialize(question)}}}]}}]}}";
            var resp = await _httpClient.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { SetResponse(idx, $"{S("err")} Gemini ({(int)resp.StatusCode}): {json}", BrushError); return; }
            using var doc = JsonDocument.Parse(json);
            SetResponse(idx, doc.RootElement.GetProperty("candidates")[0].GetProperty("content")
                .GetProperty("parts")[0].GetProperty("text").GetString() ?? "", BrushAnswer);
        }
        catch (Exception ex) { SetResponse(idx, $"{S("err")}: {ex.Message}", BrushError); }
    }

    private async Task AskDeepSeekAsync(int idx, string question, string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "deepseek-chat",
                    messages = new[] { new { role = "user", content = question } },
                    stream = false
                }), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Authorization", $"Bearer {key}");
            var resp = await _httpClient.SendAsync(req);
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { SetResponse(idx, $"{S("err")} DeepSeek ({(int)resp.StatusCode}): {json}", BrushError); return; }
            using var doc = JsonDocument.Parse(json);
            SetResponse(idx, doc.RootElement.GetProperty("choices")[0].GetProperty("message")
                .GetProperty("content").GetString() ?? "", BrushAnswer);
        }
        catch (Exception ex) { SetResponse(idx, $"{S("err")}: {ex.Message}", BrushError); }
    }

    private async Task AskPerplexityAsync(int idx, string question, string key)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.perplexity.ai/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "llama-3.1-sonar-small-128k-online",
                    messages = new[] { new { role = "user", content = question } }
                }), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Authorization", $"Bearer {key}");
            var resp = await _httpClient.SendAsync(req);
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { SetResponse(idx, $"{S("err")} Perplexity ({(int)resp.StatusCode}): {json}", BrushError); return; }
            using var doc = JsonDocument.Parse(json);
            SetResponse(idx, doc.RootElement.GetProperty("choices")[0].GetProperty("message")
                .GetProperty("content").GetString() ?? "", BrushAnswer);
        }
        catch (Exception ex) { SetResponse(idx, $"{S("err")}: {ex.Message}", BrushError); }
    }

    private void SetResponse(int idx, string text, WMedia.Brush brush)
    {
        Dispatcher.Invoke(() => { aiRows[idx].ResponseBrush = brush; aiRows[idx].Response = text; });
    }

    private void SaveAllResponses()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{S("save_header")} — {DateTime.Now:dd.MM.yyyy HH:mm}");
        sb.AppendLine(new string('═', 60));
        sb.AppendLine($"{S("question")} {txAiQuestion.Text.Trim()}");
        sb.AppendLine();
        bool hasAny = false;
        foreach (var r in aiRows)
        {
            string txt = r.Response.Trim();
            if (string.IsNullOrEmpty(txt)) continue;
            sb.AppendLine(new string('─', 60));
            sb.AppendLine($"■ {r.Name}");
            sb.AppendLine();
            sb.AppendLine(txt);
            sb.AppendLine();
            hasAny = true;
        }
        if (!hasAny) { MessageBox.Show(S("nothing_msg"), S("empty_title"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        SaveToFile(sb.ToString(), $"AI_{DateTime.Now:yyyyMMdd_HHmm}.txt");
    }

    private void SaveSingleResponse(AiRow r)
    {
        string txt = r.Response.Trim();
        if (string.IsNullOrEmpty(txt)) { MessageBox.Show(S("nothing_msg"), S("empty_title"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var sb = new StringBuilder();
        sb.AppendLine($"{r.Name} — {DateTime.Now:dd.MM.yyyy HH:mm}");
        sb.AppendLine(new string('═', 60));
        sb.AppendLine($"{S("question")} {txAiQuestion.Text.Trim()}");
        sb.AppendLine();
        sb.AppendLine(txt);
        SaveToFile(sb.ToString(), $"{r.Name}_{DateTime.Now:yyyyMMdd_HHmm}.txt");
    }

    private static void SaveToFile(string content, string fileName)
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Utilities");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content, Encoding.UTF8);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
    }

    private void ShowApiKeyDialog()
    {
        var dlg = new Window
        {
            Title = S("keys_title"), Width = 580, Height = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
            ResizeMode = ResizeMode.NoResize, Background = Frozen(30, 38, 48)
        };
        var panel = new StackPanel { Margin = new Thickness(16) };

        TextBox Row(string label, string linkText, string linkUrl, string value)
        {
            panel.Children.Add(new TextBlock
            {
                Text = label, FontWeight = FontWeights.Bold, FontSize = 13,
                Foreground = BrushAnswer, Margin = new Thickness(0, 8, 0, 2)
            });
            var tx = new TextBox { Text = value, FontSize = 13, Height = 30, Padding = new Thickness(4, 3, 4, 3) };
            panel.Children.Add(tx);
            var link = new TextBlock { Margin = new Thickness(0, 3, 0, 4), FontSize = 11 };
            var hl = new Hyperlink(new Run(linkText)) { NavigateUri = new Uri(linkUrl) };
            hl.RequestNavigate += (_, e) => Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            link.Inlines.Add(hl);
            panel.Children.Add(link);
            return tx;
        }

        var tc = Row("Claude (Anthropic) API:", S("k_claude"), "https://console.anthropic.com/settings/keys", _claudeApiKey);
        var tg = Row("Gemini API:",     S("k_gemini"),     "https://aistudio.google.com/apikey",     _geminiApiKey);
        var td = Row("DeepSeek API:",   S("k_deepseek"),   "https://platform.deepseek.com/api_keys", _deepSeekApiKey);
        var tp = Row("Perplexity API:", S("k_perplexity"), "https://www.perplexity.ai/settings/api", _perplexityApiKey);

        var btnOk = new Button
        {
            Content = S("keys_save"), Width = 140, Height = 36,
            HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0),
            FontWeight = FontWeights.Bold, IsDefault = true
        };
        btnOk.Click += (_, _) =>
        {
            _claudeApiKey = tc.Text.Trim(); _geminiApiKey = tg.Text.Trim();
            _deepSeekApiKey = td.Text.Trim(); _perplexityApiKey = tp.Text.Trim();
            SaveAiSettings();
            dlg.DialogResult = true;
        };
        panel.Children.Add(btnOk);
        dlg.Content = panel;
        dlg.ShowDialog();
    }

    private void LoadAiSettings()
    {
        string path = Path.Combine(AiDataDir, "ai_settings.json");
        if (!File.Exists(path)) return;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("ClaudeKey",     out var c)) _claudeApiKey     = c.GetString() ?? "";
            if (root.TryGetProperty("GeminiKey",     out var g)) _geminiApiKey     = g.GetString() ?? "";
            if (root.TryGetProperty("DeepSeekKey",   out var d)) _deepSeekApiKey   = d.GetString() ?? "";
            if (root.TryGetProperty("PerplexityKey", out var p)) _perplexityApiKey = p.GetString() ?? "";
        }
        catch { }
    }

    private void SaveAiSettings()
    {
        Directory.CreateDirectory(AiDataDir);
        string path = Path.Combine(AiDataDir, "ai_settings.json");
        var obj = new
        {
            ClaudeKey = _claudeApiKey, GeminiKey = _geminiApiKey,
            DeepSeekKey = _deepSeekApiKey, PerplexityKey = _perplexityApiKey
        };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
    }
}
