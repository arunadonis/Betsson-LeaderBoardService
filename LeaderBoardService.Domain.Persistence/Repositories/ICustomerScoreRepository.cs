namespace LeaderBoardService.Domain.Persistence.Repositories;

public interface ICustomerScoreRepository<TEntity> : IDataRepository<TEntity>
{
    Task<List<TEntity>> GetAllByGameIdAsync(Guid gameId, CancellationToken cancellationToken);
}