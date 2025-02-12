namespace LeaderBoardService.Common.Events;

public class CustomerScoreEventMessage : Message
{
    public Guid GameId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int Score { get; set; }
}