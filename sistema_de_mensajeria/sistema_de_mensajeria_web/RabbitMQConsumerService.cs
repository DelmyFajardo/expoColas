using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SistemaDeMensajeriaWeb;

public class RabbitMQConsumerService : BackgroundService
{
    private readonly ConcurrentQueue<string> _messageQueue;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string QueueName = "mensajes_queue";

    public RabbitMQConsumerService(ConcurrentQueue<string> messageQueue, ILogger<RabbitMQConsumerService> logger)
    {
        _messageQueue = messageQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int retryCount = 0;
        const int maxRetries = 10;
        const int retryDelayMs = 5000;
        
        while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                // Conexión a RabbitMQ
                var factory = new ConnectionFactory()
                {
                    HostName = "rabbitmq",
                    //HostName = "localhost",
                    Port = 5672,
                    UserName = "dfajardo",
                    Password = "df2025"
                };
    
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync();
    
                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken
                );
    
                _logger.LogInformation("RabbitMQ consumer service initialized successfully");
                
                var consumer = new AsyncEventingBasicConsumer(_channel);
    
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
    
                    _logger.LogInformation("Message received: {Message}", message);
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
                    var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
                    _messageQueue.Enqueue($"{localTime:yyyy-MM-dd HH:mm:ss}: {message}");
                    
                    await Task.CompletedTask;
                };
    
                await _channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: true,
                    consumer: consumer,
                    cancellationToken: stoppingToken
                );
    
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
                
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Failed to connect to RabbitMQ after {Retries} retries", maxRetries);
                    throw;
                }
                
                _logger.LogWarning("Connection to RabbitMQ failed. Retrying in {Delay}ms. Attempt {Count}/{MaxRetries}", 
                    retryDelayMs, retryCount, maxRetries);
                
                await Task.Delay(retryDelayMs, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer service");
        
        if (_channel != null && _channel.IsOpen)
            await _channel.CloseAsync(cancellationToken);
        
        if (_connection != null && _connection.IsOpen)
            await _connection.CloseAsync(cancellationToken);
            
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _connection?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }
}