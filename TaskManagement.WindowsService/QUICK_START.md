# Quick Start Guide - Task Management Windows Service

Get the Windows Service up and running in 5 minutes!

## Step 1: Install RabbitMQ

### Using Chocolatey (Easiest)
```powershell
# Open PowerShell as Administrator
choco install rabbitmq
```

### Manual Installation
1. Download Erlang: https://www.erlang.org/downloads
2. Download RabbitMQ: https://www.rabbitmq.com/download.html
3. Install both (Erlang first, then RabbitMQ)

### Verify RabbitMQ is Running
```powershell
# Check service status
Get-Service RabbitMQ

# Should show "Running"
```

## Step 2: Apply Database Migration

The service requires a database migration for concurrency control and reminder tracking.

```powershell
# Navigate to the API project
cd c:\Users\leonu\source\repos\Backend\TaskManagement.API

# Apply the migration
dotnet ef database update

# You should see: "Done."
```

## Step 3: Run the Service (Development Mode)

```powershell
# Navigate to the Windows Service project
cd c:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService

# Run the service in console mode
dotnet run --environment Development
```

You should see output like:
```
Task Reminder Publisher Service started. Check interval: 00:02:00
Task Reminder Consumer Service started
Successfully subscribed to task reminders queue
```

## Step 4: Test with an Overdue Task

### Option A: Using the API Swagger UI

1. Start the API:
```powershell
cd c:\Users\leonu\source\repos\Backend\TaskManagement.API
dotnet run
```

2. Open Swagger: http://localhost:5000/swagger
3. Use POST /api/tasks to create a task with a past due date:

```json
{
  "title": "Test Overdue Task",
  "description": "Testing the reminder service",
  "dueDate": "2024-01-01T10:00:00Z",
  "priority": 1,
  "fullName": "John Doe",
  "telephone": "+1234567890",
  "email": "john@example.com"
}
```

### Option B: Using PowerShell

```powershell
$body = @{
    title = "Test Overdue Task"
    description = "Testing the reminder service"
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

## Step 5: Verify It Works

Within 2 minutes (development check interval), you should see logs in the console:

```
Published reminder for Task ID 1 - 'Test Overdue Task' (Due: 2024-01-01 10:00)
Hi your Task is due {Task 1 - Test Overdue Task} - Due Date: 2024-01-01 10:00:00, Assigned to: John Doe (john@example.com)
```

## Step 6: Check RabbitMQ (Optional)

1. Open RabbitMQ Management UI: http://localhost:15672
2. Login: `guest` / `guest`
3. Go to "Queues" tab
4. You should see `task_reminders` queue

## Install as Windows Service (Production)

Once you've verified it works, install it as a Windows Service:

```powershell
# Run PowerShell as Administrator

# Build and publish
cd c:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService
dotnet publish -c Release -r win-x64 --self-contained

# Install as service
sc.exe create "TaskManagementReminderService" `
  binPath="C:\Users\leonu\source\repos\Backend\TaskManagement.WindowsService\bin\Release\net8.0\win-x64\publish\TaskManagement.WindowsService.exe" `
  start=auto `
  DisplayName="Task Management Reminder Service"

# Start the service
sc.exe start "TaskManagementReminderService"

# Verify it's running
Get-Service -Name "TaskManagementReminderService"
```

## Troubleshooting

### RabbitMQ Won't Start
```powershell
# Restart RabbitMQ service
Restart-Service RabbitMQ
```

### Service Can't Connect to Database
- Verify the connection string in `appsettings.json`
- Ensure SQL Server is running
- Check that migrations were applied

### No Logs Appearing
- Wait for the check interval (2 minutes in dev, 5 in production)
- Ensure you created a task with a past due date
- Check that the task is not marked as completed

### Port 5672 Already in Use
```powershell
# Check what's using the port
netstat -ano | findstr :5672
```

## Next Steps

- Read the full [README.md](README.md) for detailed configuration
- Adjust check intervals in `appsettings.json`
- Set up monitoring and alerting for production
- Configure email/SMS notifications in the consumer service

## Configuration Quick Reference

**Development (Fast Testing):**
```json
{
  "TaskReminder": {
    "CheckIntervalMinutes": 1,
    "DeduplicationWindowMinutes": 30
  }
}
```

**Production (Balanced):**
```json
{
  "TaskReminder": {
    "CheckIntervalMinutes": 5,
    "DeduplicationWindowMinutes": 60
  }
}
```

## Success Checklist

- [ ] RabbitMQ installed and running
- [ ] Database migration applied
- [ ] Service runs in console mode without errors
- [ ] Created test overdue task
- [ ] Saw reminder logs in console
- [ ] Verified queue in RabbitMQ management UI
- [ ] (Optional) Installed as Windows Service

## Need Help?

Check the [README.md](README.md) for:
- Detailed troubleshooting
- Production deployment guide
- Performance tuning
- Security recommendations
