using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentAssertions.Execution;
using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence.Repositories;
using LeaderBoardService.Service;
using Moq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LeaderBoardService.Tests
{
    public class CustomerScoreEventMessageHandlerTests : LeaderBoardServiceTestsBase
    {
        private readonly Mock<IDataRepository<Game>> _gameRepository = new();
        private readonly Mock<ICustomerScoreRepository<CustomerScore>> _customerScoreRepository = new();
        private readonly Mock<IPublisher> _publisher = new();

        private readonly CustomerScoreEventMessageHandler _controller;

        private readonly Game _game;
        private readonly CustomerScore _customerScore;

        private readonly CustomerScoreEventMessage _message;

        public CustomerScoreEventMessageHandlerTests()
        {
            _game = Fixture.Create<Game>();
            _game.Leaders = null;

            _customerScore = Fixture.Build<CustomerScore>()
                .With(x => x.GameId, _game.GameId)
                .Create();
            
            _message = new CustomerScoreEventMessage()
            {
                GameId = _game.GameId,
                CustomerId = _customerScore.CustomerId,
                CustomerName = Fixture.Create<string>(),
                Score = Fixture.Create<int>()
            };

            _gameRepository.Setup(x => x.GetByGuidAsync(_game.GameId, CancellationToken))
                .ReturnsAsync(_game);
            _customerScoreRepository.Setup(x => x.GetByGameIdAsync(_customerScore.CustomerId, _customerScore.GameId, CancellationToken))
                .ReturnsAsync(_customerScore);
            _customerScoreRepository.Setup(x => x.GetAllByGameIdAsync(_game.GameId, CancellationToken))
                .ReturnsAsync([_customerScore]);
            _controller = new CustomerScoreEventMessageHandler(_gameRepository.Object, _customerScoreRepository.Object, _publisher.Object);
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

            _gameRepository.Verify(x => x.GetByGuidAsync(_game.GameId, CancellationToken), Times.Once);
            _customerScoreRepository.Verify(x => x.GetByGameIdAsync(_customerScore.CustomerId, _customerScore.GameId, CancellationToken), Times.Once);
            _customerScoreRepository.Verify(x => x.GetAllByGameIdAsync(_game.GameId, CancellationToken), Times.Once);
            _gameRepository.Verify(x => x.SaveChangesAsync(CancellationToken), Times.Once);

            _gameRepository.VerifyNoOtherCalls();
            _customerScoreRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ItShouldAddCustomerToRepository()
        {
            _customerScoreRepository.Setup(x => x.GetByGameIdAsync(_customerScore.CustomerId, _customerScore.GameId, CancellationToken))
                .ReturnsAsync(() => (CustomerScore)null);

            await _controller.Handle(_message);

            _customerScoreRepository.Verify(x => x.AddAsync(It.Is<CustomerScore>(c =>
                    c.Score == _message.Score &&
                    c.CustomerId == _message.CustomerId &&
                    c.CustomerName == _message.CustomerName &&
                    c.GameId == _message.GameId),
                CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ItShouldNotUpdateCustomerScoreWhenNewScoreIsLower()
        {
            _message.Score = _customerScore.Score - 1;

            await _controller.Handle(_message);

            _customerScore.Score.Should().NotBe(_message.Score);
        }

        [Fact]
        public async Task ItShouldUpdateCustomerScoreWhenNewScoreIsHigher()
        {
            _message.Score = _customerScore.Score + 10;
            
            await _controller.Handle(_message);

            _customerScore.Score.Should().Be(_message.Score);
        }

        [Fact]
        public async Task ItShouldAddCustomerToLeaderBoardWhenNone()
        {
            await _controller.Handle(_message);

            var leaders = JsonSerializer.Deserialize<List<Leaders>>(_game.Leaders);
            var expected = new Leaders()
            {
                CustomerId = _customerScore.CustomerId,
                CustomerName = _customerScore.CustomerName,
                Score = _customerScore.Score,
                Rank = 1
            };

            using (new AssertionScope())
            {
                leaders.Count.Should().Be(1);
                leaders[0].Should().BeEquivalentTo(expected);
            }
        }


        [Fact]
        public async Task ItShouldPublishLeaderBoardChangeMessage()
        {
            await _controller.Handle(_message);

            JsonSerializer.Deserialize<List<Leaders>>(_game.Leaders);
            
            _publisher.Verify(x => x.Publish(It.Is<LeaderBoardEventMessage>(l =>
                l.CustomerName == _message.CustomerName &&
                l.Score == _message.Score &&
                l.Rank == 1)));
        }

        [Fact]
        public async Task ItShouldNotUpdateLeaderBoardWhenNewScoreIsLessThanPreviousScoreOfLeader()
        {
            var existingLeader = new Leaders()
            {
                CustomerId = _message.CustomerId,
                CustomerName = _message.CustomerName,
                Score = _message.Score + 100,
                Rank = 1
            };
            var existingLeaders = new List<Leaders>() { existingLeader };
            _game.Leaders = JsonSerializer.SerializeToUtf8Bytes(existingLeaders, JsonSerializerOptions.Default);

            await _controller.Handle(_message);

            var leaders = JsonSerializer.Deserialize<List<Leaders>>(_game.Leaders);

            using (new AssertionScope())
            {
                leaders.Count.Should().Be(1);
                leaders[0].Should().BeEquivalentTo(existingLeader);
            }
        }

        [Fact]
        public async Task ItShouldAddCustomerToLeaderBoard()
        {
            var newCustomerScore = Fixture.Build<CustomerScore>()
                .With(x => x.GameId, _game.GameId)
                .With(x => x.Score, _customerScore.Score - 10)
                .Create();

            _customerScoreRepository.Setup(x => x.GetByGuidAsync(newCustomerScore.CustomerId, CancellationToken))
                .ReturnsAsync(newCustomerScore);

            _customerScoreRepository.Setup(x => x.GetAllByGameIdAsync(_game.GameId, CancellationToken))
                .ReturnsAsync([_customerScore, newCustomerScore]);

            var existingLeader = new Leaders()
            {
                CustomerId = _customerScore.CustomerId,
                CustomerName = _customerScore.CustomerName,
                Score = _customerScore.Score,
                Rank = 1
            };
            var existingLeaders = new List<Leaders>() { existingLeader };
            _game.Leaders = JsonSerializer.SerializeToUtf8Bytes(existingLeaders, JsonSerializerOptions.Default);

            var message = new CustomerScoreEventMessage()
            {
                GameId = newCustomerScore.GameId,
                CustomerId = newCustomerScore.CustomerId,
                CustomerName = newCustomerScore.CustomerName,
                Score = _customerScore.Score - 10
            };

            await _controller.Handle(message);

            var leaders = JsonSerializer.Deserialize<List<Leaders>>(_game.Leaders);
            var expected = new Leaders()
            {
                CustomerId = newCustomerScore.CustomerId,
                CustomerName = newCustomerScore.CustomerName,
                Score = newCustomerScore.Score,
                Rank = 2
            };

            using (new AssertionScope())
            {
                leaders.Count.Should().Be(2);
                leaders[0].Should().BeEquivalentTo(existingLeader);
                leaders[1].Should().BeEquivalentTo(expected);
            }
        }
    }
}
