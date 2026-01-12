using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskManagement.WindowsService.Interfaces;
using TaskManagement.WindowsService.Models;

namespace TaskManagement.WindowsService.Services
{
    /// <summary>
    /// Service for handling RabbitMQ operations (publishing and consuming messages)
    /// </summary>
    public class RabbitMqService : IRabbitMqService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqService> _logger;
        private const string QueueName = "task_reminders";

        public RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration)
        {
            _logger = logger;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest",
                    
                    // Connection resilience settings
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    RequestedHeartbeat = TimeSpan.FromSeconds(60)
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                // Declare queue with durability for reliability
                _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                ).GetAwaiter().GetResult();

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
        }

        public void PublishTaskReminder(TaskReminderMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true, // Survive broker restarts
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    ContentType = "application/json"
                };

                _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: QueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                ).GetAwaiter().GetResult();

                _logger.LogInformation(
                    "Published reminder for Task ID {TaskId} - {Title}",
                    message.TaskId,
                    message.Title
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish reminder for Task ID {TaskId}",
                    message.TaskId
                );
                throw;
            }
        }

        public void StartConsuming(Action<TaskReminderMessage> onMessageReceived)
        {
            try
            {
                // Set prefetch count to control concurrent processing
                // This helps with load distribution and prevents overwhelming the consumer
                _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false).GetAwaiter().GetResult();

                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    TaskReminderMessage? message = null;
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        message = JsonSerializer.Deserialize<TaskReminderMessage>(json);

                        if (message != null)
                        {
                            // Process the message
                            onMessageReceived(message);

                            // Acknowledge message only after successful processing
                            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);

                            _logger.LogDebug(
                                "Successfully processed and acknowledged message for Task ID {TaskId}",
                                message.TaskId
                            );
                        }
                        else
                        {
                            _logger.LogWarning("Received null message, rejecting");
                            await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing message for Task ID {TaskId}",
                            message?.TaskId ?? 0
                        );

                        // Reject and requeue the message for retry
                        // In production, you might want to implement a dead letter queue
                        // after certain number of retries
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                // Start consuming with manual acknowledgment
                _channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer
                ).GetAwaiter().GetResult();

                _logger.LogInformation("Started consuming messages from queue: {QueueName}", QueueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consuming messages");
                throw;
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
                _logger.LogError(ex, "Error closing RabbitMQ connection");
            }
        }
    }
}
