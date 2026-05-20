using System.Collections.ObjectModel;

namespace ComicPlate.App.ViewModels;

public sealed record ReaderStripItemBuildResult(
    ObservableCollection<ReaderStripItemViewModel> Items,
    IReadOnlySet<int> ActivePageIndexes);
