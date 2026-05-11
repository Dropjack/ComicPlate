namespace ComicPlate.Core.Books;

public interface IBookshelfSource
{
    string RootPath { get; }

    Task<Bookshelf> LoadAsync(CancellationToken cancellationToken);
}
