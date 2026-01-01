# Quick Setup Guide - Task Management API

This guide will help you set up and run the Task Management API in just a few steps.

## ✅ Prerequisites Check

Before starting, ensure you have:

1. **Visual Studio 2022** or **.NET 8 SDK** installed
2. **SQL Server LocalDB** (comes with Visual Studio)

### Verify LocalDB Installation

Open PowerShell or Command Prompt and run:
```bash
sqllocaldb info
```

If you see `MSSQLLocalDB` in the list, you're good to go!

## 🚀 5-Minute Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/leon7713/TaskManagement.git
cd TaskManagement
```

### Step 2: Open the Project

**Option A: Visual Studio**
- Open `TaskManagement.API.sln` or `TaskManagement.API.csproj`
- Press `F5` to run

**Option B: Command Line**
```bash
cd TaskManagement.API
dotnet restore
dotnet run
```

### Step 3: Verify Setup

Once the application starts:
1. ✅ Database is created automatically
2. ✅ Migrations are applied automatically
3. ✅ Swagger UI opens in your browser

## 🎯 What Just Happened?

The application automatically:
- ✅ Created the `TaskManagementDb` database
- ✅ Applied all migrations
- ✅ Created the `Tasks` table with proper schema
- ✅ Set up indexes for performance
- ✅ Started the API server
- ✅ Opened Swagger documentation

## 📝 Try It Out!

### Using Swagger UI

1. Navigate to: `https://localhost:{port}/swagger`
2. Click on **POST /api/tasks** → **Try it out**
3. Use this sample data:

```json
{
  "title": "My First Task",
  "description": "Testing the API setup",
  "dueDate": "2024-12-31T23:59:59Z",
  "priority": 3,
  "fullName": "John Doe",
  "telephone": "+1234567890",
  "email": "john.doe@example.com"
}
```

4. Click **Execute**
5. You should see a `201 Created` response!

### Using curl

```bash
curl -X POST "https://localhost:7123/api/tasks" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My First Task",
    "description": "Testing the API setup",
    "dueDate": "2024-12-31T23:59:59Z",
    "priority": 3,
    "fullName": "John Doe",
    "telephone": "+1234567890",
    "email": "john.doe@example.com"
  }'
```

### Using PowerShell

```powershell
$body = @{
    title = "My First Task"
    description = "Testing the API setup"
    dueDate = "2024-12-31T23:59:59Z"
    priority = 3
    fullName = "John Doe"
    telephone = "+1234567890"
    email = "john.doe@example.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7123/api/tasks" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"
```

## 🔍 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | Get all tasks |
| GET | `/api/tasks/{id}` | Get specific task |
| POST | `/api/tasks` | Create new task |
| PUT | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |

## 🎨 What Makes This Setup Easy?

### 1. **Zero Manual Database Setup**
```csharp
// This code in Program.cs handles everything automatically:
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
    context.Database.Migrate();  // Creates DB + applies migrations
}
```

### 2. **LocalDB = No SQL Server Installation Required**
- Uses LocalDB that comes with Visual Studio
- No separate SQL Server installation needed
- Perfect for development

### 3. **Swagger = Interactive Documentation**
- Test all endpoints without additional tools
- See request/response schemas
- Try out the API immediately

### 4. **Comprehensive Validation**
- All inputs validated automatically
- Clear error messages
- Data integrity ensured

## 🛠️ Troubleshooting

### Problem: Port Already in Use

**Solution**: Change the port in `Properties/launchSettings.json`

```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:7124;http://localhost:5001"
    }
  }
}
```

### Problem: LocalDB Not Found

**Solution**: Install SQL Server LocalDB
1. Download from: https://aka.ms/ssmsfullsetup
2. Or run: `winget install Microsoft.SQLServer.LocalDB`

### Problem: Database Connection Error

**Solution**: Check connection string in `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Problem: Migration Errors

**Solution**: Delete the database and restart the app
```bash
sqllocaldb stop MSSQLLocalDB
sqllocaldb delete MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

Then run the app again - it will recreate everything automatically!

## 📚 Next Steps

1. **Explore the API** using Swagger
2. **Check the logs** in the console for SQL queries
3. **Modify a task** using PUT endpoint
4. **View the database** using SQL Server Object Explorer in Visual Studio

## 💡 Pro Tips

### View SQL Queries in Console

The app is configured to show SQL queries in development mode. You'll see:
```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (45ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT [t].[Id], [t].[Title], [t].[Description] ...
```

### Access Database in Visual Studio

1. Open **View** → **SQL Server Object Explorer**
2. Expand **(localdb)\MSSQLLocalDB**
3. Find **TaskManagementDb**
4. Browse tables and data

### Test with Sample Data

Run this PowerShell script to create multiple tasks:

```powershell
$tasks = @(
    @{title="Task 1"; description="First task"; priority=1},
    @{title="Task 2"; description="Second task"; priority=2},
    @{title="Task 3"; description="Third task"; priority=3}
)

foreach ($task in $tasks) {
    $body = @{
        title = $task.title
        description = $task.description
        dueDate = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ")
        priority = $task.priority
        fullName = "Test User"
        telephone = "+1234567890"
        email = "test@example.com"
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "https://localhost:7123/api/tasks" `
        -Method Post `
        -Body $body `
        -ContentType "application/json"
}
```

## ✨ Summary

You now have:
- ✅ A fully functional REST API
- ✅ Automatic database setup
- ✅ Interactive API documentation
- ✅ Comprehensive validation
- ✅ Structured error handling
- ✅ Ready for frontend integration

**Total setup time: < 5 minutes** 🎉

For more details, see the main [README.md](README.md)
