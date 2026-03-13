namespace Ecommerce.Application.Common.Interfaces;

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}