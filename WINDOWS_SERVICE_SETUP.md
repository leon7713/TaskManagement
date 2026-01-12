# Windows Service & RabbitMQ Setup Guide

This guide covers the setup and implementation of the **Task Management Windows Service** with RabbitMQ integration for handling overdue task reminders.

## Overview

The Windows Service implements the bonus requirements:

1. ✅ **Publisher Service**: Pulls overdue tasks from the database and publishes reminders to RabbitMQ queue
2. ✅ **Consumer Service**: Subscribes to the queue and logs each reminder: "Hi your Task is due {Task xxxxx}"
3. ✅ **Concurrency Handling**: Implements multiple strategies for handling concurrent updates effectively

## Project Structure

```
TaskManagement.WindowsService/
├── BackgroundServices/
│   ├── TaskReminderPublisherService.cs    # Publishes overdue tasks to queue
│   └── TaskReminderConsumerService.cs     # Consumes and logs reminders
├── Interfaces/
│   └── IRabbitMqService.cs                # RabbitMQ service interface
├── Models/
│   └── TaskReminderMessage.cs             # Message model for queue
├── Services/
│   └── RabbitMqService.cs                 # RabbitMQ implementation
├── Program.cs                              # Service entry point
├── appsettings.json                        # Configuration
├── appsettings.Development.json            # Dev configuration
├── README.md                               # Detailed documentation
├── QUICK_START.md                          # Quick start guide
└── TaskManagement.WindowsService.csproj    # Project file
```

## Key Features Implemented

### 1. Overdue Task Detection & Publishing

**TaskReminderPublisherService.cs** periodically:
- Queries database for tasks where `DueDate < DateTime.UtcNow` and `IsCompleted = false`
- Creates `TaskReminderMessage` objects
- Publishes to RabbitMQ `task_reminders` queue
- Configurable check interval (default: 5 minutes)

### 2. Message Consumption & Logging

**TaskReminderConsumerService.cs**:
- Subscribes to the `task_reminders` queue
- Logs each reminder in the format: "Hi your Task is due {Task ID - Title}"
- Implements deduplication to prevent duplicate notifications
- Handles message acknowledgment properly

### 3. Concurrency Control Mechanisms

#### A. Optimistic Concurrency (Database Level)
- Added `RowVersion` field to `TaskItem` model
- Configured as concurrency token in Entity Framework
- Prevents conflicting updates to the same task

```csharp
[Timestamp]
public byte[]? RowVersion { get; set; }
```

#### B. Message Deduplication (Application Level)
- Tracks processed task IDs with timestamps
- Configurable deduplication window (default: 60 minutes)
- Prevents duplicate notifications within the time window

```csharp
private readonly ConcurrentDictionary<int, DateTime> _processedTasks;
```

#### C. RabbitMQ Reliability Features
- **Durable Queues**: Survive broker restarts
- **Persistent Messages**: Written to disk
- **Publisher Confirms**: Ensures message delivery
- **Manual Acknowledgment**: Only removes messages after successful processing
- **Prefetch Count (10)**: Controls concurrent message processing

#### D. TaskReminder Tracking (Optional Enhancement)
- New `TaskReminder` table tracks sent reminders
- Can be used for audit trail and advanced deduplication
- Includes status tracking: "Sent", "Processed", "Failed"

## Database Changes

### New Migration: AddConcurrencyAndReminderTracking

**Changes to TaskItem:**
```sql
ALTER TABLE Tasks ADD RowVersion rowversion NOT NULL;
```

**New TaskReminders Table:**
```sql
CREATE TABLE TaskReminders (
    Id int IDENTITY(1,1) PRIMARY KEY,
    TaskId int NOT NULL,
    SentAt datetime2 NOT NULL,
    Status nvarchar(50) NOT NULL,
    Notes nvarchar(500) NULL,
    FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
);

CREATE INDEX IX_TaskReminders_TaskId ON TaskReminders(TaskId);
CREATE INDEX IX_TaskReminders_SentAt ON TaskReminders(SentAt);
CREATE INDEX IX_TaskReminders_TaskId_SentAt ON TaskReminders(TaskId, SentAt);
```

## Installation Steps

### 1. Prerequisites

- [x] .NET 8.0 SDK
- [x] SQL Server (same as API)
- [x] RabbitMQ Server

### 2. Install RabbitMQ

**Windows (Chocolatey):**
```powershell
choco install rabbitmq
```

**Verify:**
```powershell
Get-Service RabbitMQ
# Should show "Running"
```

### 3. Apply Database Migration

```powershell
cd c:\Users\leonu\source\repos\Backend\TaskManagement.API
dotnet ef database update
```

### 4. Build the Windows Service

```powershell
cd c:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService
dotnet build
```

### 5. Test in Console Mode

```powershell
dotnet run --environment Development
```

Expected output:
```
Task Reminder Publisher Service started. Check interval: 00:02:00
Task Reminder Consumer Service started
Successfully subscribed to task reminders queue
RabbitMQ connection established successfully
```

### 6. Install as Windows Service

```powershell
# Run as Administrator
dotnet publish -c Release -r win-x64 --self-contained

sc.exe create "TaskManagementReminderService" `
  binPath="C:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService\bin\Release\net8.0\win-x64\publish\TaskManagement.WindowsService.exe" `
  start=auto `
  DisplayName="Task Management Reminder Service"

sc.exe start "TaskManagementReminderService"
```

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

### Environment-Specific Settings

**Development** (`appsettings.Development.json`):
- CheckIntervalMinutes: 2 (faster testing)
- DeduplicationWindowMinutes: 30
- More verbose logging

**Production** (`appsettings.json`):
- CheckIntervalMinutes: 5 (balanced)
- DeduplicationWindowMinutes: 60
- Warning-level logging

## Testing the Implementation

### 1. Create Overdue Task

```powershell
$body = @{
    title = "Overdue Test Task"
    description = "This task is overdue"
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

### 2. Watch for Logs

**Console Mode:**
```
Published reminder for Task ID 1 - 'Overdue Test Task' (Due: 2024-01-01 10:00)
Hi your Task is due {Task 1 - Overdue Test Task} - Due Date: 2024-01-01 10:00:00, Assigned to: John Doe (john@example.com)
```

**Windows Service Mode:**
- Open Event Viewer (eventvwr.msc)
- Navigate to: Windows Logs → Application
- Filter by source: "TaskManagementReminderService"

### 3. Verify RabbitMQ

1. Open http://localhost:15672 (guest/guest)
2. Go to "Queues" tab
3. Check `task_reminders` queue exists
4. Monitor message rates

## Concurrency Testing

### Test Scenario 1: Multiple Service Instances

Run multiple instances to test concurrent processing:

```powershell
# Terminal 1
dotnet run --environment Development

# Terminal 2
dotnet run --environment Development --urls "http://localhost:5001"
```

**Expected Behavior:**
- Both instances process messages
- Deduplication prevents duplicate logs
- Messages are distributed via RabbitMQ prefetch

### Test Scenario 2: Concurrent Task Updates

```powershell
# Create task
$taskId = 1

# Update 1
Invoke-RestMethod -Uri "http://localhost:5000/api/tasks/$taskId" -Method Put -Body $body1 -ContentType "application/json"

# Update 2 (concurrent)
Invoke-RestMethod -Uri "http://localhost:5000/api/tasks/$taskId" -Method Put -Body $body2 -ContentType "application/json"
```

**Expected Behavior:**
- RowVersion ensures only one update succeeds
- Conflicting update receives 409 Conflict or retry

### Test Scenario 3: Message Redelivery

1. Stop service while processing message
2. Message should be requeued
3. Restart service
4. Message should be reprocessed

## Monitoring & Maintenance

### Service Management

```powershell
# Check status
Get-Service -Name "TaskManagementReminderService"

# Start/Stop/Restart
Start-Service -Name "TaskManagementReminderService"
Stop-Service -Name "TaskManagementReminderService"
Restart-Service -Name "TaskManagementReminderService"
```

### View Logs

```powershell
# Recent logs from Event Viewer
Get-EventLog -LogName Application -Source "TaskManagementReminderService" -Newest 20
```

### Monitor RabbitMQ

```powershell
# Check queue depth
# Visit http://localhost:15672/#/queues/%2F/task_reminders

# Or use rabbitmqctl
rabbitmqctl list_queues name messages consumers
```

## Production Recommendations

### 1. Security
- Use strong RabbitMQ credentials (not guest/guest)
- Enable TLS for RabbitMQ connections
- Store secrets in Azure Key Vault or similar
- Run service with least-privilege account

### 2. Monitoring
- Set up health checks
- Alert on service failures
- Monitor queue depth
- Track message processing rates
- Set up dead letter queue (DLQ)

### 3. Performance
- Adjust check interval based on load
- Tune RabbitMQ prefetch count
- Consider database query optimization
- Implement message batching for high volume

### 4. Reliability
- Set up RabbitMQ clustering
- Implement circuit breakers
- Add retry policies with exponential backoff
- Configure database connection pooling

## Troubleshooting

### Service Won't Start

```powershell
# Check Event Viewer
Get-EventLog -LogName Application -Source "TaskManagementReminderService" -Newest 5

# Common issues:
# - RabbitMQ not running
# - Database connection failed
# - Port conflicts
```

### No Messages Published

```powershell
# Verify overdue tasks exist
# Connect to SQL Server and run:
SELECT * FROM Tasks WHERE DueDate < GETUTCDATE() AND IsCompleted = 0;
```

### Messages Not Consumed

```powershell
# Check RabbitMQ queue has consumers
# Visit http://localhost:15672/#/queues
# Look for consumer count > 0
```

## Architecture Benefits

✅ **Scalability**: Multiple service instances can run concurrently  
✅ **Reliability**: Messages persist through restarts  
✅ **Decoupling**: Publisher and consumer are independent  
✅ **Concurrency**: Multiple strategies prevent conflicts  
✅ **Observability**: Comprehensive logging and monitoring  
✅ **Maintainability**: Clean separation of concerns  

## Next Steps

1. **Enhance Notifications**: Add email/SMS sending in consumer
2. **Add Health Checks**: Implement IHealthCheck interface
3. **Metrics**: Add Prometheus/Grafana monitoring
4. **Dead Letter Queue**: Handle failed messages
5. **Message Prioritization**: Priority queue for urgent tasks
6. **Batch Processing**: Process multiple tasks in batches

## Additional Resources

- [Full README](TaskManagement.WindowsService/README.md)
- [Quick Start Guide](TaskManagement.WindowsService/QUICK_START.md)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [.NET Background Services](https://docs.microsoft.com/en-us/dotnet/core/extensions/windows-service)

## Summary

The Windows Service successfully implements all bonus requirements:

1. ✅ **Requirement 1**: Service pulls overdue tasks and publishes to RabbitMQ queue
2. ✅ **Requirement 2**: Service subscribes to queue and logs reminders
3. ✅ **Requirement 3**: Implements comprehensive concurrency handling:
   - Optimistic concurrency with RowVersion
   - Application-level deduplication
   - RabbitMQ reliability features
   - Optional TaskReminder tracking

The implementation is production-ready with proper error handling, logging, and configuration management.
