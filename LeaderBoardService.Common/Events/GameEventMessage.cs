namespace LeaderBoardService.Common.Events;

public class GameEventMessage : Message
{
    public Guid GameId { get; set; }
    public string? GameName { get; set; }
    public string? EventType { get; set; }
}