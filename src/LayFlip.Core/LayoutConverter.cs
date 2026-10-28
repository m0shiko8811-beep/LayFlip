namespace LayFlip.Core;

public enum ConversionDirection
{
    EnglishToHebrew,
    HebrewToEnglish,
}

public readonly record struct ConversionResult(string Text, ConversionDirection Direction);

/// <summary>
/// Converts text typed in the wrong keyboard layout between English (US) and
/// the standard Israeli Hebrew layout (SI-1452).
/// </summary>
public static class LayoutConverter
{
    // Key on the English layout -> character the same key produces on the Hebrew layout.
    private static readonly (char En, char He)[] Table =
    [
        ('q', '/'), ('w', '\''), ('e', 'ק'), ('r', 'ר'), ('t', 'א'),
        ('y', 'ט'), ('u', 'ו'), ('i', 'ן'), ('o', 'ם'), ('p', 'פ'),
        ('a', 'ש'), ('s', 'ד'), ('d', 'ג'), ('f', 'כ'), ('g', 'ע'),
        ('h', 'י'), ('j', 'ח'), ('k', 'ל'), ('l', 'ך'), (';', 'ף'), ('\'', ','),
        ('z', 'ז'), ('x', 'ס'), ('c', 'ב'), ('v', 'ה'), ('b', 'נ'),
        ('n', 'מ'), ('m', 'צ'), (',', 'ת'), ('.', 'ץ'), ('/', '.'),
    ];

    // Punctuation appears on both sides of the table, so each direction gets its
    // own one-way dictionary and a single conversion only ever uses one of them.
    public static IReadOnlyDictionary<char, char> EnglishToHebrew { get; } =
        Table.ToDictionary(p => p.En, p => p.He);

    public static IReadOnlyDictionary<char, char> HebrewToEnglish { get; } =
        Table.ToDictionary(p => p.He, p => p.En);

    public static bool ContainsHebrewLetter(string text)
    {
        foreach (var c in text)
        {
            if (c >= 'א' && c <= 'ת')
                return true;
        }
        return false;
    }

    public static ConversionResult Convert(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (ContainsHebrewLetter(text))
            return new ConversionResult(Map(text, HebrewToEnglish, lowerAscii: false), ConversionDirection.HebrewToEnglish);

        return new ConversionResult(Map(text, EnglishToHebrew, lowerAscii: true), ConversionDirection.EnglishToHebrew);
    }

    private static string Map(string text, IReadOnlyDictionary<char, char> map, bool lowerAscii)
    {
        var chars = new char[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var key = lowerAscii && c >= 'A' && c <= 'Z' ? (char)(c + 32) : c;
            chars[i] = map.TryGetValue(key, out var mapped) ? mapped : c;
        }
        return new string(chars);
    }
}
