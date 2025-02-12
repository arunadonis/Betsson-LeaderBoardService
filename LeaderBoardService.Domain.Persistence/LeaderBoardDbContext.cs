using LeaderBoardService.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoardService.Domain.Persistence;

public class LeaderBoardDbContext : DbContext
{
    public LeaderBoardDbContext(DbContextOptions options) : base(options) { }
    public DbSet<Game> Games { get; set; }
    public DbSet<CustomerScore> CustomerScores { get; set; }
    public DbSet<LeaderBoard> LeaderBoards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>().HasData(
            new Game()
            {
                Id = 1,
                GameId = Guid.Parse("3B584072-2173-45FC-A733-94F3537641A5"),
                GameName = "Cricket",
                IsActive = false
            },
            new Game()
            {
                Id = 2,
                GameId = Guid.Parse("9E5FC146-8700-44FD-95E8-465992561923"),
                GameName = "Tennis",
                IsActive = false
            });
    }
}