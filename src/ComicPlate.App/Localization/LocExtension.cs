using Avalonia.Markup.Xaml;
using ComicPlate.App.Services;

namespace ComicPlate.App.Localization;

public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizationService.Current.GetString(Key);
    }
}
