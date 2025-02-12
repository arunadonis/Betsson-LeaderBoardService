using System.Text.Json;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence.Repositories;

namespace LeaderBoardService.Service
{
    public class LeaderBoardService : ILeaderBoardService
    {
        private readonly IDataRepository<Game> _dataRepository;

        public LeaderBoardService(IDataRepository<Game> dataRepository)
        {
            _dataRepository = dataRepository;
        }

        public async Task<List<Leaders>> GetLeadersAsync(Guid gameId, CancellationToken cancellationToken)
        {
            var game = await _dataRepository.GetByGuidAsync(gameId, cancellationToken);

            if (!game.IsActive) return [];

            var leaders = JsonSerializer.Deserialize<List<Leaders>>(game.Leaders);

            return leaders is not null ? leaders.OrderByDescending(x => x.Score).ToList() : [];

        }
    }
}
