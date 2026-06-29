using System.Text;

namespace SeniorUtilities.Tools;

/// <summary>
/// Конвертация текста между раскладками RU (ЙЦУКЕН) и EN (US QWERTY).
/// Точный порт таблиц из layoutfix.py. Направление определяется автоматически:
/// есть кириллица — переводим RU→EN, иначе EN→RU.
/// </summary>
public static class LayoutConverter
{
    private const string EnLetters = "qwertyuiopasdfghjklzxcvbnm";
    private const string RuLetters = "йцукенгшщзфывапролдячсмить";

    // Знаки препинания без Shift
    private static readonly (string En, string Ru)[] Punct =
    {
        ("[", "х"), ("]", "ъ"), (";", "ж"), ("'", "э"),
        (",", "б"), (".", "ю"), ("/", "."), ("`", "ё"),
    };

    // Знаки препинания с Shift
    private static readonly (string En, string Ru)[] PunctShift =
    {
        ("{", "Х"), ("}", "Ъ"), (":", "Ж"), ("\"", "Э"),
        ("<", "Б"), (">", "Ю"), ("?", ","), ("~", "Ё"),
    };

    // Верхний ряд цифр с Shift, где символы различаются
    private static readonly (string En, string Ru)[] NumRowShift =
    {
        ("@", "\""), ("#", "№"), ("$", ";"), ("^", ":"), ("&", "?"),
    };

    private static readonly Dictionary<char, char> En2Ru = BuildEn2Ru();
    private static readonly Dictionary<char, char> Ru2En = BuildReverse(En2Ru);

    private static Dictionary<char, char> BuildEn2Ru()
    {
        var table = new Dictionary<char, char>();
        for (int i = 0; i < EnLetters.Length; i++)
        {
            char e = EnLetters[i], r = RuLetters[i];
            table[e] = r;
            table[char.ToUpper(e)] = char.ToUpper(r);
        }
        foreach (var (en, ru) in Punct.Concat(PunctShift).Concat(NumRowShift))
            table[en[0]] = ru[0];
        return table;
    }

    private static Dictionary<char, char> BuildReverse(Dictionary<char, char> src)
    {
        var rev = new Dictionary<char, char>();
        foreach (var kv in src)
            rev[kv.Value] = kv.Key; // при конфликте остаётся последнее — как в Python dict-comprehension
        return rev;
    }

    public static bool HasCyrillic(string text)
    {
        foreach (char ch in text)
            if (ch >= 'Ѐ' && ch <= 'ӿ')
                return true;
        return false;
    }

    /// <summary>Меняет раскладку всего текста. Направление — по наличию кириллицы.</summary>
    public static string Convert(string text)
    {
        var table = HasCyrillic(text) ? Ru2En : En2Ru;
        var sb = new StringBuilder(text.Length);
        foreach (char ch in text)
            sb.Append(table.TryGetValue(ch, out var mapped) ? mapped : ch);
        return sb.ToString();
    }
}
