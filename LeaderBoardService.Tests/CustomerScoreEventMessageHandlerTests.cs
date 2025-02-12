using AutoFixture;
using LeaderBoardService.Common.Events;
using LeaderBoardService.Common.Messaging;
using LeaderBoardService.Domain.Model;
using LeaderBoardService.Domain.Persistence.Repositories;
using LeaderBoardService.Service;
using Moq;

namespace LeaderBoardService.Tests
{
    public class CustomerScoreEventMessageHandlerTests : LeaderBoardServiceTestsBase
    {
        private readonly Mock<IDataRepository<Game>> _gameRepository = new();
        private readonly Mock<ICustomerScoreRepository<CustomerScore>> _customerScoreRepository = new();
        private readonly Mock<RabbitMqPublisher> _publisher = new();
        private readonly CustomerScoreEventMessageHandler _controller;

        private readonly Game _game;
        private readonly CustomerScore _customerScore;

        private readonly Guid _customerId;
        private readonly Guid _gameId;
        private readonly CustomerScoreEventMessage _message;

        public CustomerScoreEventMessageHandlerTests()
        {
            _game = Fixture.Create<Game>();
            _customerScore = Fixture.Create<CustomerScore>();

            _gameId = Fixture.Create<Guid>();
            _customerId = Fixture.Create<Guid>();

            _message = new CustomerScoreEventMessage()
            {
                GameId = _gameId,
                CustomerId = _customerId,
                CustomerName = Fixture.Create<string>(),
                Score = Fixture.Create<int>()
            };

            _gameRepository.Setup(x => x.GetByGuidAsync(_gameId, CancellationToken))
                .ReturnsAsync(_game);
            _customerScoreRepository.Setup(x => x.GetByGuidAsync(_gameId, CancellationToken))
                .ReturnsAsync(_customerScore);
            _controller = new CustomerScoreEventMessageHandler(_gameRepository.Object, _customerScoreRepository.Object, _publisher.Object);
        }
    }
}
