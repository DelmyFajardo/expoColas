using System.Text;
using RabbitMQ.Client;

async Task Main()
{
    // Conexión a RabbitMQ
    var factory = new ConnectionFactory()
    {
        //HostName = "rabbitmq",
        HostName = "localhost",
        Port = 5672,
        UserName = "dfajardo",
        Password = "df2025"
    };
    
    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();
    
    string queueName = "mensajes_queue";
    
    await channel.QueueDeclareAsync(queue: queueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    Console.WriteLine("Sistema de Mensajería con RabbitMQ");
    Console.WriteLine("Escriba un mensaje......");
    
    while (true)
    {
        Console.Write("> ");
        string? mensaje = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(mensaje) || mensaje.ToLower() == "salir")
            break;
    
        var body = Encoding.UTF8.GetBytes(mensaje);
        
        await channel.BasicPublishAsync(
            exchange: String.Empty, 
            routingKey: queueName,
            body: body,
            CancellationToken.None
        );
        Console.WriteLine($"Mensaje enviado: {mensaje}");
    }
    
    Console.WriteLine("Saliendo del programa...");
}

await Main();