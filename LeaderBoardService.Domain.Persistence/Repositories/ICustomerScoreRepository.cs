namespace LeaderBoardService.Domain.Persistence.Repositories;

public interface ICustomerScoreRepository<TEntity> : IDataRepository<TEntity>
{
    Task<TEntity> GetByGameIdAsync(Guid customerId, Guid gameId, CancellationToken cancellationToken);
    Task<List<TEntity>> GetAllByGameIdAsync(Guid gameId, CancellationToken cancellationToken);
}