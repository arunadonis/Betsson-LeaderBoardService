using AutoFixture;
using FluentAssertions;
using LeaderBoardService.Common.Events;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence.Repositories;
using LeaderBoardService.Service;
using Moq;

namespace LeaderBoardService.Tests;

public class GameEventMessageHandlerTests : LeaderBoardServiceTestsBase
{
    private readonly Mock<IDataRepository<Game>> _gameRepository = new();
    private readonly GameEventMessageHandler _controller;

    private readonly Game _game;
    private readonly Guid _gameId;
    private readonly GameEventMessage _message;

    public GameEventMessageHandlerTests()
    {
        _game = Fixture.Create<Game>();
        _gameId = Fixture.Create<Guid>();
        _message = new GameEventMessage()
        {
            GameId = _gameId,
            GameName = Fixture.Create<string>(),
            EventType = "START"
        };

        _gameRepository.Setup(x => x.GetByGuidAsync(_gameId, CancellationToken))
            .ReturnsAsync(_game);
        _controller = new GameEventMessageHandler(_gameRepository.Object);
    }

    [Fact]
    public async Task ItShouldThrowExceptionWhenGameNotFound()
    {
        var gameId = Fixture.Create<Guid>();
        _message.GameId = gameId;

        _gameRepository.Setup(x => x.GetByGuidAsync(gameId, CancellationToken))
            .ReturnsAsync(() => (Game)null);

        var act = async () => await _controller.Handle(_message);

        var res = await act.Should().ThrowAsync<InvalidOperationException>();
        res.And.Message.Should().Contain($"Game not found for the given id {gameId}");
    }

    [Fact]
    public async Task ItShouldGetDataFromRepository()
    {
        await _controller.Handle(_message);

        _gameRepository.Verify(x => x.GetByGuidAsync(_gameId, CancellationToken), Times.Once);
        _gameRepository.Verify(x => x.SaveChangesAsync(CancellationToken), Times.Once);
        _gameRepository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("START")]
    [InlineData("END")]
    public async Task ItShouldChangeGameToActiveStatus(string eventType)
    {
        _message.EventType = eventType;

        await _controller.Handle(_message);

        switch (eventType)
        {
            case "START":
                _game.IsActive.Should().BeTrue();
                break;
            case "END":
                _game.IsActive.Should().BeFalse();
                break;
        }
    }
}