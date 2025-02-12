namespace LeaderBoardService.Service;

public interface ILeaderBoardService
{
    Task<List<Leaders>> GetLeadersAsync(Guid gameId, CancellationToken cancellationToken);
}