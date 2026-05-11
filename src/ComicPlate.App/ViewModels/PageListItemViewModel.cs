using ComicPlate.Core.Books;

namespace ComicPlate.App.ViewModels;

public sealed class PageListItemViewModel
{
    public PageListItemViewModel(int index, PageEntry page)
    {
        Index = index;
        Page = page;
    }

    public int Index { get; }

    public int DisplayIndex => Index + 1;

    public string FileName => Page.DisplayName;

    public PageEntry Page { get; }
}
