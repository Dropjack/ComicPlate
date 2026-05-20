using System.Globalization;
using System.Text;
using ComicPlate.App.Services;
using ComicPlate.Infrastructure.Persistence;

namespace ComicPlate.Tests.Services;

public sealed class LocalizationServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"ComicPlateLocalizationTests-{Guid.NewGuid():N}");

    public LocalizationServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(
            Path.Combine(_tempDirectory, "en.json"),
            """
            {
              "Greeting": "Hello",
              "OnlyEnglish": "Fallback"
            }
            """,
            Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(_tempDirectory, "zh-Hans.json"),
            """
            {
              "Greeting": "Ni hao"
            }
            """,
            Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(_tempDirectory, "ja.json"),
            """
            {
              "Greeting": "Konnichiwa"
            }
            """,
            Encoding.UTF8);
    }

    [Fact]
    public void ResolveLanguageTagUsesSupportedSystemLanguage()
    {
        Assert.Equal(
            LocalizationService.SimplifiedChineseTag,
            LocalizationService.ResolveLanguageTag(AppLanguage.System, CultureInfo.GetCultureInfo("zh-CN")));
        Assert.Equal(
            LocalizationService.JapaneseTag,
            LocalizationService.ResolveLanguageTag(AppLanguage.System, CultureInfo.GetCultureInfo("ja-JP")));
    }

    [Fact]
    public void ResolveLanguageTagFallsBackToEnglishForUnsupportedSystemLanguage()
    {
        Assert.Equal(
            LocalizationService.EnglishTag,
            LocalizationService.ResolveLanguageTag(AppLanguage.System, CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void GetStringFallsBackToEnglishWhenSelectedLanguageMissingKey()
    {
        var service = LocalizationService.Create(
            AppLanguage.SimplifiedChinese,
            localizationDirectory: _tempDirectory);

        Assert.Equal("Ni hao", service.GetString("Greeting"));
        Assert.Equal("Fallback", service.GetString("OnlyEnglish"));
    }

    [Fact]
    public void GetStringFallsBackToKeyWhenAllLanguagesAreMissingKey()
    {
        var service = LocalizationService.Create(
            AppLanguage.Japanese,
            localizationDirectory: _tempDirectory);

        Assert.Equal("Missing.Key", service.GetString("Missing.Key"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
