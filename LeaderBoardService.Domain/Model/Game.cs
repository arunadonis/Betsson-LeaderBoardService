using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderBoardService.Domain.Model;

public class Game
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Guid GameId { get; set; }
    public string? GameName { get; set; }
    public bool IsActive { get; set; }
    public byte[]? Leaders { get; set; }
}