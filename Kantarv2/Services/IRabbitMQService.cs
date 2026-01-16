namespace Kantarv2.Services
{
    public interface IRabbitMQService
    {
        // Queue operations
        Task CreateQueueAsync(string queueName, CancellationToken cancellationToken = default);
        Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
        Task<string?> ConsumeAsync(string queueName, CancellationToken cancellationToken = default);

        // Exchange operations
        Task DeclareExchangeAsync(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false, CancellationToken cancellationToken = default);
        Task BindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default);
        Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default);
    }
}
