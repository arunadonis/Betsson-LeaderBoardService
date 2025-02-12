using AutoFixture;
using AutoFixture.AutoMoq;

namespace LeaderBoardService.Tests;

public abstract class LeaderBoardServiceTestsBase
{
    protected static IFixture Fixture { get; } = new Fixture()
        .Customize(new AutoMoqCustomization() { ConfigureMembers = true, GenerateDelegates = true });
    protected readonly CancellationToken CancellationToken = new();
}