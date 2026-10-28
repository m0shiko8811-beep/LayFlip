using LayFlip.Core;

namespace LayFlip.Core.Tests;

public class LayoutConverterTests
{
    [Fact]
    public void EnglishToHebrew_Shalom()
    {
        var result = LayoutConverter.Convert("akuo");
        Assert.Equal("שלום", result.Text);
        Assert.Equal(ConversionDirection.EnglishToHebrew, result.Direction);
    }

    [Fact]
    public void HebrewToEnglish_Shalom()
    {
        var result = LayoutConverter.Convert("שלום");
        Assert.Equal("akuo", result.Text);
        Assert.Equal(ConversionDirection.HebrewToEnglish, result.Direction);
    }

    [Fact]
    public void Uppercase_MapsLikeLowercase()
    {
        Assert.Equal("שלום", LayoutConverter.Convert("AKUO").Text);
    }

    [Fact]
    public void Table_IsBijective()
    {
        Assert.Equal(31, LayoutConverter.EnglishToHebrew.Count);
        Assert.Equal(31, LayoutConverter.HebrewToEnglish.Count);
    }

    public static TheoryData<char, char> TablePairs()
    {
        var data = new TheoryData<char, char>();
        foreach (var (en, he) in LayoutConverter.EnglishToHebrew)
            data.Add(en, he);
        return data;
    }

    [Theory]
    [MemberData(nameof(TablePairs))]
    public void EveryKey_RoundTripsFromEnglish(char en, char he)
    {
        // A lone punctuation key has no Hebrew letter, so it always goes English to Hebrew.
        Assert.Equal(he.ToString(), LayoutConverter.Convert(en.ToString()).Text);

        // Prefix a Hebrew letter to force the Hebrew to English direction for every key.
        var back = LayoutConverter.Convert("ש" + he).Text;
        Assert.Equal("a" + en, back);
    }

    [Theory]
    [MemberData(nameof(TablePairs))]
    public void EveryKey_RoundTripsFromHebrew(char en, char he)
    {
        var english = LayoutConverter.Convert("ש" + he);
        Assert.Equal(ConversionDirection.HebrewToEnglish, english.Direction);
        Assert.Equal("a" + en, english.Text);

        var hebrew = LayoutConverter.Convert(english.Text);
        Assert.Equal(ConversionDirection.EnglishToHebrew, hebrew.Direction);
        Assert.Equal("ש" + he, hebrew.Text);
    }

    [Fact]
    public void WholeKeyboardRow_RoundTrips()
    {
        const string english = "qwertyuiopasdfghjkl;'zxcvbnm,./";
        var hebrew = LayoutConverter.Convert(english);
        Assert.Equal("/'קראטוןםפשדגכעיחלךף,זסבהנמצתץ.", hebrew.Text);
        Assert.Equal(english, LayoutConverter.Convert(hebrew.Text).Text);
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("   ")]
    [InlineData("[]{}()")]
    [InlineData("\r\n\t")]
    [InlineData("!@#$%^&*-_=+")]
    public void UnmappedCharacters_PassThrough_EnglishDirection(string input)
    {
        Assert.Equal(input, LayoutConverter.Convert(input).Text);
    }

    [Fact]
    public void UnmappedCharacters_PassThrough_HebrewDirection()
    {
        Assert.Equal("a 123 [x] (y) {z}\n", ReplaceHebrewA("ש 123 [x] (y) {z}\n"));

        static string ReplaceHebrewA(string s) => LayoutConverter.Convert(s).Text;
    }

    [Fact]
    public void Brackets_AreNotMapped()
    {
        Assert.Equal("[ש]", LayoutConverter.Convert("[a]").Text);
        Assert.Equal("(a)", LayoutConverter.Convert("(ש)").Text);
    }

    [Fact]
    public void MixedSentence_WithSpaces()
    {
        Assert.Equal("שלום עולם", LayoutConverter.Convert("akuo guko").Text);
        Assert.Equal("akuo guko", LayoutConverter.Convert("שלום עולם").Text);
        Assert.Equal("שלום, מה נשמע?", LayoutConverter.Convert("akuo' nv bang?").Text);
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        var result = LayoutConverter.Convert("");
        Assert.Equal("", result.Text);
        Assert.Equal(ConversionDirection.EnglishToHebrew, result.Direction);
    }

    [Fact]
    public void HebrewToEnglish_OutputIsLowercase()
    {
        var text = LayoutConverter.Convert("קראטוןםפשדגכעיחלךזסבהנמצ").Text;
        Assert.Equal(text.ToLowerInvariant(), text);
    }
}
