using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderBoardService.Domain.Model;

public class CustomerScore
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid GameId { get; set; }    
    public string? CustomerName { get; set; }
    public int Score { get; set; }
}