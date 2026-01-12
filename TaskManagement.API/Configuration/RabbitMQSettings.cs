namespace TaskManagement.API.Configuration
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string ExchangeName { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
    }
}
