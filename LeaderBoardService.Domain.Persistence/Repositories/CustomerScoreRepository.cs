using System;
using LeaderBoardService.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoardService.Domain.Persistence.Repositories
{
    public class CustomerScoreRepository : ICustomerScoreRepository<CustomerScore>
    {
        readonly LeaderBoardDbContext _leaderBoardDbContext;

        public CustomerScoreRepository(LeaderBoardDbContext leaderBoardDbContext)
        {
            _leaderBoardDbContext = leaderBoardDbContext;
        }

        public async Task<IEnumerable<CustomerScore>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _leaderBoardDbContext.CustomerScores.ToListAsync(cancellationToken);
        }

        public async Task<CustomerScore> GetAsync(int id, CancellationToken cancellationToken)
        {
            return await _leaderBoardDbContext.CustomerScores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<CustomerScore> GetByGuidAsync(Guid guid, CancellationToken cancellationToken)
        {
            return await _leaderBoardDbContext.CustomerScores.FirstOrDefaultAsync(x => x.CustomerId == guid, cancellationToken);
        }

        public async Task<CustomerScore> GetByGameIdAsync(Guid customerId, Guid gameId, CancellationToken cancellationToken)
        {
            return await _leaderBoardDbContext.CustomerScores.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.GameId == gameId, cancellationToken);
        }

        public async Task<List<CustomerScore>> GetAllByGameIdAsync(Guid gameId, CancellationToken cancellationToken)
        {
            return await _leaderBoardDbContext.CustomerScores.Where(x => x.GameId == gameId).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(CustomerScore entity, CancellationToken cancellationToken)
        {
            await _leaderBoardDbContext.CustomerScores.AddAsync(entity, cancellationToken);
            await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(CustomerScore dbEntity, CustomerScore entity, CancellationToken cancellationToken)
        {
            if (dbEntity.CustomerId == entity.CustomerId)
            {
                dbEntity.GameId = entity.GameId;
                dbEntity.CustomerName = entity.CustomerName;
                dbEntity.Score = dbEntity.Score;

                await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task DeleteAsync(CustomerScore entity, CancellationToken cancellationToken)
        {
            _leaderBoardDbContext.CustomerScores.Remove(entity);
            await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
