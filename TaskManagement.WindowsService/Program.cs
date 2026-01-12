using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.WindowsService.BackgroundServices;
using TaskManagement.WindowsService.Interfaces;
using TaskManagement.WindowsService.Services;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Task Management Reminder Service";
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Add DbContext with connection string from configuration
        services.AddDbContext<TaskManagementDbContext>(options =>
            options.UseSqlServer(
                hostContext.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }
            )
        );

        // Add RabbitMQ Service as Singleton (maintains persistent connection)
        services.AddSingleton<IRabbitMqService, RabbitMqService>();

        // Add Background Services
        services.AddHostedService<TaskReminderPublisherService>();
        services.AddHostedService<TaskReminderConsumerService>();

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            
            // Add Event Log only on Windows
            if (OperatingSystem.IsWindows())
            {
                logging.AddEventLog(settings =>
                {
                    settings.SourceName = "TaskManagementReminderService";
                });
            }
        });
    })
    .Build();

// Log startup
var logger = builder.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Task Management Reminder Service is starting...");

await builder.RunAsync();

logger.LogInformation("Task Management Reminder Service has stopped");
