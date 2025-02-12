namespace LeaderBoardService.Common.Events;

public class LeaderBoardEventMessage : Message
{
    public string? GameName { get; set; }
    public string? CustomerName { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
}