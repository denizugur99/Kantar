using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Kantarv2.Services
{
    public class RabbitMQService : IRabbitMQService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;
            var uri = configuration["RabbitMQ:Uri"]
                ?? throw new ArgumentNullException("RabbitMQ:Uri configuration is missing");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(uri)
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _logger.LogInformation("RabbitMQ connection established");
        }

        #region Queue Operations

        public async Task CreateQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Queue '{QueueName}' declared", queueName);
        }

        public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        {
            await CreateQueueAsync(queueName, cancellationToken);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Message published to queue '{QueueName}'", queueName);
        }

        public async Task<string?> ConsumeAsync(string queueName, CancellationToken cancellationToken = default)
        {
            await CreateQueueAsync(queueName, cancellationToken);

            var result = await _channel.BasicGetAsync(queueName, autoAck: true, cancellationToken);

            if (result == null)
            {
                return null;
            }

            var message = Encoding.UTF8.GetString(result.Body.ToArray());
            _logger.LogInformation("Message consumed from queue '{QueueName}'", queueName);

            return message;
        }

        #endregion

        #region Exchange Operations

        public async Task DeclareExchangeAsync(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false, CancellationToken cancellationToken = default)
        {
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: exchangeType,
                durable: durable,
                autoDelete: autoDelete,
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Exchange '{ExchangeName}' declared with type '{ExchangeType}'", exchangeName, exchangeType);
        }

        public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey, CancellationToken cancellationToken = default)
        {
            await CreateQueueAsync(queueName, cancellationToken);

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey,
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Queue '{QueueName}' bound to exchange '{ExchangeName}' with routing key '{RoutingKey}'", queueName, exchangeName, routingKey);
        }

        public async Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Message published to exchange '{ExchangeName}' with routing key '{RoutingKey}'", exchangeName, routingKey);
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            if (_channel.IsOpen)
            {
                await _channel.CloseAsync();
            }

            if (_connection.IsOpen)
            {
                await _connection.CloseAsync();
            }

            _logger.LogInformation("RabbitMQ connection closed");
        }
    }
}
