using LeaderBoardService.Service;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoardService.Controllers;

[ApiController]
[Route("[controller]")]
public class LeaderBoardController : ControllerBase
{
    private readonly ILeaderBoardService _leaderBoardService;

    public LeaderBoardController(ILeaderBoardService leaderBoardService)
    {
        _leaderBoardService = leaderBoardService;
    }

    [HttpGet]
    public async Task<List<Leaders>> GetAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var leaderBoard = await _leaderBoardService.GetLeadersAsync(gameId, cancellationToken);
        return leaderBoard;
    }
}