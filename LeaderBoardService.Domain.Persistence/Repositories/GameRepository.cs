using LeaderBoardService.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoardService.Domain.Persistence.Repositories;

public class GameRepository : IDataRepository<Game>
{
    readonly LeaderBoardDbContext _leaderBoardDbContext;

    public GameRepository(LeaderBoardDbContext leaderBoardDbContext)
    {
        _leaderBoardDbContext = leaderBoardDbContext;
    }

    public async Task<IEnumerable<Game>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _leaderBoardDbContext.Games.ToListAsync(cancellationToken);
    }

    public async Task<Game> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _leaderBoardDbContext.Games.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Game> GetByGuidAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await _leaderBoardDbContext.Games.FirstOrDefaultAsync(x => x.GameId == guid, cancellationToken);
    }

    public async Task AddAsync(Game entity, CancellationToken cancellationToken)
    {
        await _leaderBoardDbContext.Games.AddAsync(entity, cancellationToken);
        await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Game dbEntity, Game entity, CancellationToken cancellationToken)
    {
        if (dbEntity.GameId == entity.GameId)
        {
            dbEntity.GameName = entity.GameName;
            dbEntity.IsActive = entity.IsActive;

            await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Game entity, CancellationToken cancellationToken)
    {
        _leaderBoardDbContext.Games.Remove(entity);
        await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
    }
}