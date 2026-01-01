# Windows Service with RabbitMQ - Setup Guide

## Overview

The Task Management Windows Service monitors tasks for overdue status and sends reminders via RabbitMQ message queue.

## Features

- **Automated Task Monitoring**: Checks for overdue tasks every 5 minutes
- **Message Queue Integration**: Uses RabbitMQ for reliable message delivery
- **Logging**: Comprehensive logging of all actions
- **Concurrent Updates**: Handles multiple updates safely through message queuing

## Prerequisites

### Install RabbitMQ

1. **Install Erlang** (Required by RabbitMQ):
   - Download from: https://www.erlang.org/downloads
   - Install with default settings

2. **Install RabbitMQ**:
   - Download from: https://www.rabbitmq.com/download.html
   - Install with default settings

3. **Start RabbitMQ Service**:
   ```bash
   # Start RabbitMQ
   rabbitmq-service.bat start
   
   # Enable Management Plugin (Optional but recommended)
   rabbitmq-plugins.bat enable rabbitmq_management
   ```

4. **Access RabbitMQ Management UI**:
   - URL: http://localhost:15672
   - Default credentials: guest / guest

## Installation

### 1. Build the Service

```bash
cd TaskManagement.Service
dotnet restore
dotnet build
dotnet publish -c Release -o ./publish
```

### 2. Install as Windows Service

```powershell
# Run PowerShell as Administrator
sc.exe create "TaskManagementService" binPath= "C:\Path\To\TaskManagement.Service\publish\TaskManagement.Service.exe"
```

Or use the provided installation script:

```powershell
# As Administrator
.\install-service.ps1
```

### 3. Configure the Service

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YourSqlServerConnectionString"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### 4. Start the Service

```powershell
# Start service
sc.exe start TaskManagementService

# Or using Services console
services.msc
```

## How It Works

### 1. Task Monitoring
- Service runs every 5 minutes
- Queries database for tasks where `DueDate < CurrentTime` AND `IsCompleted = false`
- Publishes reminder messages to RabbitMQ queue

### 2. Message Publishing
- Creates `TaskReminderMessage` object
- Serializes to JSON
- Publishes to `task-reminders` queue
- Queue is durable (survives RabbitMQ restart)

### 3. Message Consumption
- Service subscribes to the same queue
- Receives published messages
- Logs reminder: "Hi {Name}, your Task is due - Task ID: {Id}, Title: '{Title}'"
- Acknowledges message after successful processing

### 4. Concurrent Update Handling
- Messages are queued and processed sequentially
- Prevents race conditions
- Failed messages are requeued automatically
- Database transactions ensure data consistency

## Service Management

### View Service Status
```powershell
sc.exe query TaskManagementService
```

### Stop Service
```powershell
sc.exe stop TaskManagementService
```

### Restart Service
```powershell
sc.exe stop TaskManagementService
sc.exe start TaskManagementService
```

### Uninstall Service
```powershell
sc.exe stop TaskManagementService
sc.exe delete TaskManagementService
```

## Logging

Logs are written to:
- **Console** (when running in development)
- **Windows Event Log** (when running as service)
- **File** (optional - configure in appsettings.json)

### View Logs in Event Viewer

1. Open Event Viewer (`eventvwr.msc`)
2. Navigate to: Windows Logs → Application
3. Filter by Source: `TaskManagementService`

### Example Log Messages

```
Information: Found 3 overdue tasks
Information: Published reminder for Task ID: 5 - Complete Documentation
Warning: TASK REMINDER: Hi John Doe, your Task is due - Task ID: 5, Title: 'Complete Documentation', Due Date: 12/30/2024 5:00:00 PM
```

## Configuration Options

### Check Interval

Modify in `TaskReminderWorker.cs`:
```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
```

### Queue Name

Change queue name:
```csharp
private readonly string _queueName = "task-reminders";
```

### RabbitMQ Connection

Update in `appsettings.json`:
```json
"RabbitMQ": {
  "HostName": "your-rabbitmq-server",
  "Port": "5672",
  "UserName": "your-username",
  "Password": "your-password"
}
```

## Troubleshooting

### Service Won't Start

1. Check Event Viewer for errors
2. Verify RabbitMQ is running
3. Verify database connection string
4. Ensure service account has necessary permissions

### RabbitMQ Connection Failed

```powershell
# Check if RabbitMQ is running
rabbitmqctl.bat status

# Restart RabbitMQ
rabbitmq-service.bat restart
```

### Database Connection Issues

- Verify SQL Server is running
- Check connection string
- Ensure service account has database access

### Messages Not Being Processed

1. Check RabbitMQ Management UI for queue status
2. Verify messages are in the queue
3. Check service logs for consumer errors
4. Ensure queue names match between publisher and consumer

## Testing

### Manual Testing

1. **Create Overdue Task** in the API:
   ```json
   {
     "title": "Test Overdue Task",
     "dueDate": "2024-01-01T00:00:00",
     ...
   }
   ```

2. **Wait for Service** to run (or restart service)

3. **Check Logs** for reminder message

4. **Check RabbitMQ Management UI**:
   - Go to Queues tab
   - View `task-reminders` queue
   - See messages published/consumed

### Development Testing

Run service in console mode:
```bash
cd TaskManagement.Service
dotnet run
```

This allows you to see real-time console output.

## Performance Considerations

- **Polling Interval**: Adjust based on requirements (current: 5 minutes)
- **Batch Size**: Service processes all overdue tasks per cycle
- **Queue Durability**: Messages survive broker restart
- **Message Persistence**: Enabled for reliability
- **Concurrent Processing**: Single consumer prevents race conditions

## Security Best Practices

1. **RabbitMQ Credentials**: Use strong passwords in production
2. **Database Access**: Use least-privilege service account
3. **Service Account**: Run under dedicated service account
4. **Encryption**: Enable SSL/TLS for RabbitMQ in production
5. **Connection Strings**: Use User Secrets or Azure Key Vault

## Architecture Diagram

```
┌─────────────────────┐
│  Windows Service    │
│  (Background Worker)│
└──────────┬──────────┘
           │
           │ Every 5 min
           ▼
    ┌─────────────┐
    │  SQL Server │
    │  (Query)    │
    └──────┬──────┘
           │
           │ Overdue Tasks Found
           ▼
    ┌──────────────┐
    │  RabbitMQ    │
    │  Publisher   │
    └──────┬───────┘
           │
           │ Queue: task-reminders
           ▼
    ┌──────────────┐
    │  RabbitMQ    │
    │  Consumer    │
    └──────┬───────┘
           │
           │ Process & Log
           ▼
    ┌──────────────┐
    │  Log System  │
    │  (Event Log) │
    └──────────────┘
```

## Production Deployment

### Checklist

- [ ] RabbitMQ installed and configured
- [ ] Connection strings updated
- [ ] Service built in Release mode
- [ ] Service installed on production server
- [ ] Firewall rules configured (RabbitMQ ports)
- [ ] Monitoring and alerting set up
- [ ] Backup and disaster recovery plan
- [ ] Documentation updated

### Monitoring

Set up monitoring for:
- Service uptime
- RabbitMQ queue depth
- Message processing rate
- Error rate in logs
- Database connection status

---

**Note**: Ensure RabbitMQ is installed and running before starting the Windows Service.
