# Windows Service Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Task Management System                                │
│                                                                              │
│  ┌──────────────────────┐                    ┌─────────────────────────┐   │
│  │   Web API            │                    │   Windows Service       │   │
│  │   (TaskManagement.   │                    │   (Background Process)  │   │
│  │    API)              │                    │                         │   │
│  │                      │                    │  ┌──────────────────┐   │   │
│  │  - CRUD Operations   │                    │  │   Publisher      │   │   │
│  │  - Task Management   │                    │  │   Service        │   │   │
│  │  - REST Endpoints    │                    │  │                  │   │   │
│  └──────────┬───────────┘                    │  │  Timer (5 min)   │   │   │
│             │                                 │  │      ↓           │   │   │
│             │                                 │  │  Query Overdue   │   │   │
│             ▼                                 │  │      ↓           │   │   │
│  ┌──────────────────────┐                    │  │  Publish to MQ   │   │   │
│  │   SQL Server         │◄───────────────────┼──┤      ↓           │   │   │
│  │   Database           │                    │  │  Confirm Sent    │   │   │
│  │                      │                    │  └─────────┬────────┘   │   │
│  │  - Tasks Table       │                    │            │            │   │
│  │  - TaskReminders     │                    │            │            │   │
│  │  - Concurrency       │                    │            ▼            │   │
│  │    Control           │                    │  ┌──────────────────┐   │   │
│  └──────────────────────┘                    │  │   Consumer       │   │   │
│                                               │  │   Service        │   │   │
│                                               │  │                  │   │   │
│                                               │  │  Subscribe       │   │   │
│                                               │  │      ↓           │   │   │
│                                               │  │  Receive Msg     │   │   │
│                                               │  │      ↓           │   │   │
│                                               │  │  Deduplicate     │   │   │
│                                               │  │      ↓           │   │   │
│                                               │  │  Log Reminder    │   │   │
│                                               │  │      ↓           │   │   │
│                                               │  │  Acknowledge     │   │   │
│                                               │  └─────────▲────────┘   │   │
│                                               │            │            │   │
│                                               └────────────┼────────────┘   │
│                                                            │                │
│                                                            │                │
│                              ┌─────────────────────────────┘                │
│                              │                                              │
│                              ▼                                              │
│                   ┌──────────────────────┐                                  │
│                   │   RabbitMQ Broker    │                                  │
│                   │                      │                                  │
│                   │  Queue:              │                                  │
│                   │  "task_reminders"    │                                  │
│                   │                      │                                  │
│                   │  - Durable           │                                  │
│                   │  - Persistent Msgs   │                                  │
│                   │  - Manual Ack        │                                  │
│                   │  - Prefetch: 10      │                                  │
│                   └──────────────────────┘                                  │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Component Interaction Flow

### 1. Publisher Flow (Task Detection & Publishing)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Publisher Service Workflow                         │
└─────────────────────────────────────────────────────────────────────────┘

    Timer Triggers (Every 5 minutes)
              ↓
    ┌─────────────────────┐
    │ Query Database      │
    │ WHERE DueDate < NOW │
    │ AND IsCompleted = 0 │
    └──────────┬──────────┘
              ↓
    ┌─────────────────────┐
    │ Found Overdue Tasks?│──── No ──→ Wait for next interval
    └──────────┬──────────┘
              Yes
              ↓
    ┌─────────────────────────────┐
    │ For Each Overdue Task:      │
    │                             │
    │  1. Create Message Object   │
    │     - TaskId                │
    │     - Title                 │
    │     - DueDate               │
    │     - FullName              │
    │     - Email                 │
    │     - ProcessedAt           │
    │                             │
    │  2. Publish to RabbitMQ     │
    │     - Set Persistent        │
    │     - Set MessageId         │
    │     - Set Timestamp         │
    │                             │
    │  3. Wait for Confirmation   │
    │     - Publisher Confirms    │
    │                             │
    │  4. Log Success             │
    └─────────────────────────────┘
              ↓
    ┌─────────────────────┐
    │ All Tasks Published │
    └─────────────────────┘
              ↓
    Wait for Next Timer Interval
```

### 2. Consumer Flow (Message Processing & Logging)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Consumer Service Workflow                          │
└─────────────────────────────────────────────────────────────────────────┘

    Service Starts
         ↓
    ┌─────────────────────┐
    │ Subscribe to Queue  │
    │ "task_reminders"    │
    └──────────┬──────────┘
              ↓
    ┌─────────────────────┐
    │ Wait for Messages   │◄──────────────┐
    └──────────┬──────────┘               │
              ↓                           │
    ┌─────────────────────┐               │
    │ Message Received    │               │
    └──────────┬──────────┘               │
              ↓                           │
    ┌──────────────────────────┐          │
    │ Deserialize Message      │          │
    └──────────┬───────────────┘          │
              ↓                           │
    ┌──────────────────────────┐          │
    │ Check Deduplication      │          │
    │ - Already processed?     │          │
    │ - Within time window?    │          │
    └──────────┬───────────────┘          │
              ↓                           │
         Is Duplicate?                    │
         ↙        ↘                       │
       Yes         No                     │
        ↓           ↓                     │
    Skip &    ┌─────────────────────┐    │
    Ack       │ Log Reminder        │    │
              │ "Hi your Task is    │    │
              │  due {Task...}"     │    │
              └──────────┬──────────┘    │
                        ↓                │
              ┌─────────────────────┐    │
              │ Mark as Processed   │    │
              │ - Add to cache      │    │
              │ - Store timestamp   │    │
              └──────────┬──────────┘    │
                        ↓                │
              ┌─────────────────────┐    │
              │ Acknowledge Message │    │
              │ (Remove from Queue) │    │
              └──────────┬──────────┘    │
                        ↓                │
              ┌─────────────────────┐    │
              │ Optional: Send      │    │
              │ - Email             │    │
              │ - SMS               │    │
              │ - Push Notification │    │
              └──────────┬──────────┘    │
                        ↓                │
                   Continue ─────────────┘
```

### 3. Concurrency Control Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   Concurrent Update Handling                            │
└─────────────────────────────────────────────────────────────────────────┘

Scenario: Multiple Updates to Same Task
────────────────────────────────────────

    User 1 Updates Task          User 2 Updates Task
           ↓                            ↓
    ┌──────────────┐            ┌──────────────┐
    │ Read Task    │            │ Read Task    │
    │ RowVersion=1 │            │ RowVersion=1 │
    └──────┬───────┘            └──────┬───────┘
           ↓                            ↓
    ┌──────────────┐            ┌──────────────┐
    │ Modify Data  │            │ Modify Data  │
    └──────┬───────┘            └──────┬───────┘
           ↓                            ↓
    ┌──────────────┐            ┌──────────────┐
    │ Save Changes │            │ Save Changes │
    │ WHERE        │            │ WHERE        │
    │ RowVersion=1 │            │ RowVersion=1 │
    └──────┬───────┘            └──────┬───────┘
           ↓                            ↓
    ┌──────────────┐            ┌──────────────┐
    │ ✅ SUCCESS   │            │ ❌ CONFLICT  │
    │ RowVersion=2 │            │ Row changed  │
    └──────────────┘            └──────┬───────┘
                                       ↓
                                ┌──────────────┐
                                │ Retry or     │
                                │ Return Error │
                                └──────────────┘

Scenario: Duplicate Message Processing
───────────────────────────────────────

    Message 1 Arrives        Message 2 Arrives (Duplicate)
           ↓                            ↓
    ┌──────────────┐            ┌──────────────┐
    │ Check Cache  │            │ Check Cache  │
    │ TaskId: 123  │            │ TaskId: 123  │
    │ Not Found    │            │ Found!       │
    └──────┬───────┘            └──────┬───────┘
           ↓                            ↓
    ┌──────────────┐            ┌──────────────┐
    │ Process Msg  │            │ Check Time   │
    │ Log Reminder │            │ < 60 min?    │
    └──────┬───────┘            └──────┬───────┘
           ↓                            ↓
    ┌──────────────┐                  Yes
    │ Add to Cache │                   ↓
    │ TaskId: 123  │            ┌──────────────┐
    │ Time: Now    │            │ Skip & Ack   │
    └──────┬───────┘            │ (Duplicate)  │
           ↓                    └──────────────┘
    ┌──────────────┐
    │ Acknowledge  │
    └──────────────┘

Scenario: RabbitMQ Prefetch Control
────────────────────────────────────

    Queue has 100 messages
           ↓
    ┌──────────────────────┐
    │ Consumer 1           │
    │ Prefetch: 10         │
    │ Receives 10 messages │
    └──────────────────────┘
           ↓
    ┌──────────────────────┐
    │ Consumer 2           │
    │ Prefetch: 10         │
    │ Receives 10 messages │
    └──────────────────────┘
           ↓
    Remaining 80 messages stay in queue
    until consumers acknowledge processed messages
```

## Data Models

### TaskReminderMessage (Queue Message)

```
┌─────────────────────────────────┐
│   TaskReminderMessage           │
├─────────────────────────────────┤
│ + TaskId: int                   │
│ + Title: string                 │
│ + DueDate: DateTime             │
│ + FullName: string              │
│ + Email: string                 │
│ + ProcessedAt: DateTime         │
└─────────────────────────────────┘
```

### TaskItem (Database Entity)

```
┌─────────────────────────────────┐
│   TaskItem                      │
├─────────────────────────────────┤
│ + Id: int                       │
│ + Title: string                 │
│ + Description: string           │
│ + DueDate: DateTime             │
│ + Priority: int                 │
│ + FullName: string              │
│ + Telephone: string             │
│ + Email: string                 │
│ + CreatedAt: DateTime           │
│ + UpdatedAt: DateTime?          │
│ + IsCompleted: bool             │
│ + RowVersion: byte[]            │◄─── Concurrency Token
│                                 │
│ + IsOverdue: bool (computed)    │
└─────────────────────────────────┘
```

### TaskReminder (Tracking Entity)

```
┌─────────────────────────────────┐
│   TaskReminder                  │
├─────────────────────────────────┤
│ + Id: int                       │
│ + TaskId: int                   │◄─── FK to TaskItem
│ + SentAt: DateTime              │
│ + Status: string                │
│ + Notes: string?                │
│                                 │
│ + Task: TaskItem?               │◄─── Navigation
└─────────────────────────────────┘
```

## Service Dependencies

```
┌──────────────────────────────────────────────────────────────┐
│                    Dependency Graph                          │
└──────────────────────────────────────────────────────────────┘

Program.cs
    │
    ├──► TaskManagementDbContext
    │       │
    │       └──► SQL Server Connection
    │
    ├──► RabbitMqService (Singleton)
    │       │
    │       ├──► ILogger<RabbitMqService>
    │       ├──► IConfiguration
    │       └──► RabbitMQ Connection
    │
    ├──► TaskReminderPublisherService (Hosted)
    │       │
    │       ├──► ILogger<TaskReminderPublisherService>
    │       ├──► IServiceProvider
    │       ├──► IRabbitMqService
    │       └──► IConfiguration
    │
    └──► TaskReminderConsumerService (Hosted)
            │
            ├──► ILogger<TaskReminderConsumerService>
            ├──► IRabbitMqService
            └──► IConfiguration
```

## Configuration Flow

```
┌──────────────────────────────────────────────────────────────┐
│                 Configuration Hierarchy                      │
└──────────────────────────────────────────────────────────────┘

appsettings.json (Base)
    │
    ├──► ConnectionStrings
    │       └──► DefaultConnection
    │
    ├──► RabbitMQ
    │       ├──► HostName: "localhost"
    │       ├──► Port: "5672"
    │       ├──► UserName: "guest"
    │       └──► Password: "guest"
    │
    └──► TaskReminder
            ├──► CheckIntervalMinutes: 5
            └──► DeduplicationWindowMinutes: 60

            ⬇ Overridden by ⬇

appsettings.Development.json (Dev Override)
    │
    ├──► Logging (More verbose)
    │
    └──► TaskReminder
            ├──► CheckIntervalMinutes: 2
            └──► DeduplicationWindowMinutes: 30
```

## Deployment Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Production Deployment                     │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│   Load Balancer     │
└──────────┬──────────┘
           │
    ┌──────┴──────┐
    ↓             ↓
┌────────┐   ┌────────┐
│ Web    │   │ Web    │
│ API    │   │ API    │
│ (IIS)  │   │ (IIS)  │
└───┬────┘   └───┬────┘
    │            │
    └─────┬──────┘
          ↓
    ┌──────────────┐
    │  SQL Server  │
    │  (Cluster)   │
    └──────────────┘

┌─────────────────────┐
│ Windows Service     │
│ (Instance 1)        │
└──────────┬──────────┘
           │
┌─────────────────────┐
│ Windows Service     │
│ (Instance 2)        │
└──────────┬──────────┘
           │
    ┌──────┴──────┐
    ↓             ↓
┌────────────────────┐
│  RabbitMQ Cluster  │
│  - Node 1          │
│  - Node 2          │
│  - Node 3          │
└────────────────────┘
```

## Monitoring Points

```
┌──────────────────────────────────────────────────────────────┐
│                    Monitoring Strategy                       │
└──────────────────────────────────────────────────────────────┘

1. Windows Service Health
   ├─ Service Running Status
   ├─ CPU Usage
   ├─ Memory Usage
   └─ Thread Count

2. RabbitMQ Metrics
   ├─ Queue Depth
   ├─ Message Rate (in/out)
   ├─ Consumer Count
   ├─ Unacknowledged Messages
   └─ Connection Status

3. Database Performance
   ├─ Query Execution Time
   ├─ Connection Pool Usage
   ├─ Deadlocks
   └─ Index Usage

4. Application Logs
   ├─ Error Rate
   ├─ Warning Rate
   ├─ Published Message Count
   ├─ Consumed Message Count
   └─ Deduplication Hit Rate

5. Business Metrics
   ├─ Overdue Tasks Count
   ├─ Reminders Sent
   ├─ Average Processing Time
   └─ Failed Messages
```

## Error Handling Strategy

```
┌──────────────────────────────────────────────────────────────┐
│                    Error Handling Flow                       │
└──────────────────────────────────────────────────────────────┘

Database Error
    ↓
┌─────────────────┐
│ Retry with      │
│ Exponential     │
│ Backoff         │
│ (Max 5 times)   │
└────────┬────────┘
         ↓
    Still Fails?
         ↓
┌─────────────────┐
│ Log Error       │
│ Alert Ops Team  │
│ Continue Service│
└─────────────────┘

RabbitMQ Connection Error
    ↓
┌─────────────────┐
│ Auto Recovery   │
│ Enabled         │
│ (10 sec retry)  │
└────────┬────────┘
         ↓
    Reconnects
         ↓
┌─────────────────┐
│ Resume Normal   │
│ Operation       │
└─────────────────┘

Message Processing Error
    ↓
┌─────────────────┐
│ Log Error       │
│ with Details    │
└────────┬────────┘
         ↓
┌─────────────────┐
│ NACK Message    │
│ (Requeue: true) │
└────────┬────────┘
         ↓
┌─────────────────┐
│ Message Returns │
│ to Queue        │
│ for Retry       │
└─────────────────┘
```

## Security Considerations

```
┌──────────────────────────────────────────────────────────────┐
│                    Security Layers                           │
└──────────────────────────────────────────────────────────────┘

1. Service Account
   └─ Run with least-privilege Windows account

2. Database Connection
   ├─ Use Windows Authentication (preferred)
   ├─ Or SQL Auth with strong password
   └─ Connection string encryption

3. RabbitMQ
   ├─ Strong credentials (not guest/guest)
   ├─ TLS/SSL for connections
   ├─ Virtual host isolation
   └─ User permissions (read/write specific queues)

4. Configuration
   ├─ Secrets in Azure Key Vault
   ├─ Or Windows Credential Manager
   └─ Environment-specific configs

5. Logging
   ├─ No sensitive data in logs
   ├─ Sanitize email/phone numbers
   └─ Secure log storage
```

This architecture provides a robust, scalable, and maintainable solution for task reminder notifications with comprehensive concurrency handling.
