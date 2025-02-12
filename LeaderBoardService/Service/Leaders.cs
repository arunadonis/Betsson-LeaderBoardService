namespace LeaderBoardService.Service;

public class Leaders
{
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int Score { get; set; }

    public int Rank { get; set; }
}