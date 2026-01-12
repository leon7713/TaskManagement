# Task Management Windows Service with RabbitMQ

This Windows Service monitors overdue tasks and sends reminders through RabbitMQ queues, providing a robust solution for task notification management.

## Features

✅ **Automatic Overdue Task Detection**: Periodically scans the database for tasks past their due date  
✅ **RabbitMQ Integration**: Publishes reminders to a durable queue for reliable message delivery  
✅ **Message Consumer**: Subscribes to the queue and logs reminders  
✅ **Concurrency Handling**: Implements deduplication to prevent duplicate notifications  
✅ **Optimistic Concurrency Control**: Uses RowVersion for handling concurrent database updates  
✅ **Resilient Design**: Auto-recovery for RabbitMQ connections and database retry logic  

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Windows Service                          │
│                                                             │
│  ┌──────────────────────┐      ┌─────────────────────────┐ │
│  │  Publisher Service   │      │   Consumer Service      │ │
│  │                      │      │                         │ │
│  │  1. Query DB for     │      │  1. Subscribe to queue  │ │
│  │     overdue tasks    │      │  2. Log reminders       │ │
│  │  2. Publish to queue │      │  3. Deduplicate msgs    │ │
│  └──────────┬───────────┘      └───────────▲─────────────┘ │
│             │                              │               │
└─────────────┼──────────────────────────────┼───────────────┘
              │                              │
              ▼                              │
         ┌────────────────────────────────────┐
         │         RabbitMQ Queue             │
         │      "task_reminders"              │
         │   (Durable, Persistent Messages)   │
         └────────────────────────────────────┘
```

## Prerequisites

### 1. RabbitMQ Installation

**Option A: Using Chocolatey (Recommended)**
```powershell
choco install rabbitmq
```

**Option B: Manual Installation**
1. Download and install Erlang: https://www.erlang.org/downloads
2. Download and install RabbitMQ: https://www.rabbitmq.com/download.html
3. Enable RabbitMQ Management Plugin:
```powershell
rabbitmq-plugins enable rabbitmq_management
```

**Verify Installation:**
```powershell
# Check RabbitMQ service is running
Get-Service RabbitMQ

# Access management UI (default credentials: guest/guest)
# http://localhost:15672
```

### 2. .NET 8.0 SDK
Download from: https://dotnet.microsoft.com/download/dotnet/8.0

### 3. SQL Server
The service uses the same database as the main API.

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  },
  "TaskReminder": {
    "CheckIntervalMinutes": 5,
    "DeduplicationWindowMinutes": 60
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `CheckIntervalMinutes` | How often to check for overdue tasks | 5 minutes |
| `DeduplicationWindowMinutes` | Time window to prevent duplicate notifications | 60 minutes |
| `RabbitMQ:HostName` | RabbitMQ server hostname | localhost |
| `RabbitMQ:Port` | RabbitMQ server port | 5672 |

## Building the Service

```powershell
# Navigate to the service directory
cd c:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService

# Build the project
dotnet build

# Publish for deployment (self-contained)
dotnet publish -c Release -r win-x64 --self-contained

# Or framework-dependent
dotnet publish -c Release
```

## Installation as Windows Service

### Method 1: Using sc.exe (Built-in Windows Tool)

```powershell
# Run PowerShell as Administrator

# Create the service
sc.exe create "TaskManagementReminderService" `
  binPath="C:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService\bin\Release\net8.0\win-x64\publish\TaskManagement.WindowsService.exe" `
  start=auto `
  DisplayName="Task Management Reminder Service"

# Start the service
sc.exe start "TaskManagementReminderService"

# Check service status
sc.exe query "TaskManagementReminderService"
```

### Method 2: Using PowerShell New-Service

```powershell
# Run PowerShell as Administrator

$servicePath = "C:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService\bin\Release\net8.0\win-x64\publish\TaskManagement.WindowsService.exe"

New-Service -Name "TaskManagementReminderService" `
            -BinaryPathName $servicePath `
            -DisplayName "Task Management Reminder Service" `
            -Description "Monitors overdue tasks and sends reminders via RabbitMQ" `
            -StartupType Automatic

Start-Service -Name "TaskManagementReminderService"
```

## Managing the Service

### Start/Stop/Restart

```powershell
# Start
Start-Service -Name "TaskManagementReminderService"

# Stop
Stop-Service -Name "TaskManagementReminderService"

# Restart
Restart-Service -Name "TaskManagementReminderService"

# Check status
Get-Service -Name "TaskManagementReminderService"
```

### View Logs

**Event Viewer:**
1. Open Event Viewer (eventvwr.msc)
2. Navigate to: Windows Logs → Application
3. Filter by source: "TaskManagementReminderService"

**Console Logs (Development):**
```powershell
# Run without installing as service
cd c:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService
dotnet run
```

### Uninstall Service

```powershell
# Stop the service first
Stop-Service -Name "TaskManagementReminderService"

# Delete the service
sc.exe delete "TaskManagementReminderService"
```

## Testing

### 1. Test RabbitMQ Connection

```powershell
# Access RabbitMQ Management UI
Start-Process "http://localhost:15672"

# Login with guest/guest
# Check if "task_reminders" queue is created
```

### 2. Test the Service Locally

```powershell
# Run in console mode (not as Windows Service)
cd c:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService
dotnet run --environment Development
```

### 3. Create Test Overdue Task

Using the API, create a task with a past due date:

```powershell
# Using PowerShell
$body = @{
    title = "Test Overdue Task"
    description = "This task is overdue for testing"
    dueDate = "2024-01-01T10:00:00Z"
    priority = 1
    fullName = "John Doe"
    telephone = "+1234567890"
    email = "john@example.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/tasks" `
                  -Method Post `
                  -Body $body `
                  -ContentType "application/json"
```

### 4. Verify Reminder Logs

Check the console output or Event Viewer for log entries like:
```
Hi your Task is due {Task 1 - Test Overdue Task} - Due Date: 2024-01-01 10:00:00, Assigned to: John Doe (john@example.com)
```

## Concurrency Handling

The service implements multiple strategies to handle concurrent updates:

### 1. Optimistic Concurrency Control
- `RowVersion` field added to `TaskItem` model
- Prevents conflicting updates to the same task

### 2. Message Deduplication
- Tracks processed task IDs with timestamps
- Prevents duplicate notifications within the deduplication window
- Configurable via `DeduplicationWindowMinutes`

### 3. RabbitMQ Features
- **Durable Queues**: Messages survive broker restarts
- **Persistent Messages**: Messages are written to disk
- **Publisher Confirms**: Ensures messages are received by broker
- **Manual Acknowledgment**: Messages are only removed after successful processing
- **Prefetch Count**: Controls concurrent message processing (set to 10)

## Troubleshooting

### Service Won't Start

**Check Event Viewer for errors:**
```powershell
Get-EventLog -LogName Application -Source "TaskManagementReminderService" -Newest 10
```

**Common Issues:**
1. **RabbitMQ not running**: Start RabbitMQ service
2. **Database connection failed**: Verify connection string
3. **Port already in use**: Check if another service is using port 5672

### No Messages in Queue

1. **Check database has overdue tasks:**
```sql
SELECT * FROM Tasks WHERE DueDate < GETUTCDATE() AND IsCompleted = 0
```

2. **Verify service is running:**
```powershell
Get-Service -Name "TaskManagementReminderService"
```

3. **Check RabbitMQ queue:**
- Open http://localhost:15672
- Navigate to Queues tab
- Look for "task_reminders" queue

### Messages Not Being Consumed

1. **Check consumer is subscribed:**
   - Look for log: "Started consuming messages from queue: task_reminders"

2. **Check for errors in Event Viewer**

3. **Verify RabbitMQ connection:**
```powershell
# Test RabbitMQ is accessible
Test-NetConnection -ComputerName localhost -Port 5672
```

## Production Deployment Recommendations

### 1. Security

```json
{
  "RabbitMQ": {
    "HostName": "production-rabbitmq.example.com",
    "Port": "5672",
    "UserName": "prod_user",
    "Password": "use_secure_password_or_secrets_manager"
  }
}
```

- Use strong credentials (not guest/guest)
- Enable TLS/SSL for RabbitMQ connections
- Store sensitive configuration in Azure Key Vault or similar

### 2. Monitoring

- Set up health checks
- Monitor queue depth in RabbitMQ
- Alert on service failures
- Track message processing rates

### 3. Scaling

- Run multiple consumer instances for high throughput
- Use RabbitMQ clustering for high availability
- Consider message TTL (Time To Live) for old messages

### 4. Database Optimization

```sql
-- Ensure indexes exist for performance
CREATE INDEX IX_Tasks_DueDate_IsCompleted ON Tasks(DueDate, IsCompleted);
```

### 5. Dead Letter Queue

For production, implement a Dead Letter Queue (DLQ) for failed messages:

```csharp
var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "dlx" },
    { "x-dead-letter-routing-key", "task_reminders_dlq" }
};

_channel.QueueDeclare(
    queue: QueueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: args
);
```

## Performance Tuning

### Adjust Check Interval

For high-volume systems, adjust the check interval:

```json
{
  "TaskReminder": {
    "CheckIntervalMinutes": 1  // Check every minute
  }
}
```

### Prefetch Count

Modify `RabbitMqService.cs` to adjust concurrent processing:

```csharp
// Process more messages concurrently
_channel.BasicQos(prefetchSize: 0, prefetchCount: 50, global: false);
```

## Support

For issues or questions:
1. Check Event Viewer logs
2. Review RabbitMQ management console
3. Verify database connectivity
4. Check service configuration

## License

This service is part of the Task Management System project.
