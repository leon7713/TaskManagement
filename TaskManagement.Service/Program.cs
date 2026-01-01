using Microsoft.EntityFrameworkCore;
using TaskManagement.Service.Data;
using TaskManagement.Service.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Task Management Reminder Service";
});

builder.Services.AddDbContext<TaskManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddHostedService<TaskReminderWorker>();

var host = builder.Build();
host.Run();
