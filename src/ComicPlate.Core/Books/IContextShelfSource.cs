namespace ComicPlate.Core.Books;

public interface IContextShelfSource
{
    string RootPath { get; }

    Task<ContextShelf> LoadAsync(CancellationToken cancellationToken);
}
