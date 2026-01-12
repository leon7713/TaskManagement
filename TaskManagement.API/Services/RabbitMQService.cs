using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TaskManagement.API.Configuration;
using TaskManagement.API.Interfaces;
using TaskManagement.API.Models;

namespace TaskManagement.API.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly object _lock = new object();
        private bool _initializationAttempted = false;

        public RabbitMQService(RabbitMQSettings settings, ILogger<RabbitMQService> logger)
        {
            _settings = settings;
            _logger = logger;
            // Lazy initialization - don't block startup
            _logger.LogInformation("RabbitMQService created. Connection will be established on first use.");
        }

        private void EnsureConnected()
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                return;

            lock (_lock)
            {
                // Double-check inside lock
                if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                    return;

                if (_initializationAttempted && _connection == null)
                {
                    // Already tried and failed, don't retry every time
                    return;
                }

                try
                {
                    InitializeRabbitMQ();
                    _initializationAttempted = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ not available. Application will continue without messaging.");
                    _initializationAttempted = true;
                    // Don't throw - let the app continue without RabbitMQ
                }
            }
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(3), // Add timeout
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                // Declare exchange
                _channel.ExchangeDeclareAsync(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                ).GetAwaiter().GetResult();

                // Declare queue
                _channel.QueueDeclareAsync(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                ).GetAwaiter().GetResult();

                // Bind queue to exchange
                _channel.QueueBindAsync(
                    queue: _settings.QueueName,
                    exchange: _settings.ExchangeName,
                    routingKey: "task.*"
                ).GetAwaiter().GetResult();

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
                throw; // Re-throw so EnsureConnected can catch it
            }
        }

        public void PublishTaskEvent(TaskEventMessage message)
        {
            // Fire-and-forget pattern - don't block the HTTP request
            _ = Task.Run(() => PublishTaskEventAsync(message));
        }

        private async Task PublishTaskEventAsync(TaskEventMessage message)
        {
            try
            {
                EnsureConnected();

                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogWarning("RabbitMQ not available. Skipping event publish for Task ID: {TaskId}", message.TaskId);
                    return;
                }

                var jsonMessage = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                var routingKey = $"task.{message.EventType.ToLower()}";

                await _channel.BasicPublishAsync(
                    exchange: _settings.ExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("Published task event: {EventType} for Task ID: {TaskId}", 
                    message.EventType, message.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish task event to RabbitMQ. Application continues normally.");
                // Don't throw - messaging is optional, don't break the API
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.CloseAsync().GetAwaiter().GetResult();
                _channel?.Dispose();
                _connection?.CloseAsync().GetAwaiter().GetResult();
                _connection?.Dispose();
                _logger.LogInformation("RabbitMQ connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing RabbitMQ connection");
            }
        }
    }
}
