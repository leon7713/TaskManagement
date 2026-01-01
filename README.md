# Task Management System

A full-stack web application for managing user tasks with .NET Core backend, React frontend, and SQL Server database.

## 📋 Overview

This application allows users to create, read, update, and delete tasks. Each task contains:
- Title
- Description
- Due Date
- Priority (1-5)
- User Details (Full Name, Telephone, Email)

All fields include comprehensive validation to ensure data integrity.

## 🏗️ Architecture

### Backend (.NET Core 8)
- **Framework**: ASP.NET Core 8 Web API
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (LocalDB)
- **Pattern**: Repository pattern with DTO mapping
- **Validation**: Data annotations with model validation

### Frontend (React + TypeScript)
- **Framework**: React 18 with TypeScript
- **State Management**: Redux Toolkit
- **API Client**: Axios
- **Styling**: CSS Modules
- **Form Validation**: Custom validation hooks

### Database Schema

```sql
Table: Tasks
- Id (int, Primary Key, Identity)
- Title (nvarchar(200), Required)
- Description (nvarchar(1000), Required)
- DueDate (datetime2, Required)
- Priority (int, Required, Range: 1-5)
- FullName (nvarchar(100), Required)
- Telephone (nvarchar(20), Required)
- Email (nvarchar(100), Required)
- CreatedAt (datetime2, Required)
- UpdatedAt (datetime2, Nullable)
- IsCompleted (bit, Required, Default: false)

Indexes:
- IX_Tasks_DueDate
- IX_Tasks_Email
- IX_Tasks_IsCompleted
```

## 🚀 Prerequisites

### Required Software
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or higher)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) or SQL Server
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Optional
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)
- [Postman](https://www.postman.com/) for API testing

## 📦 Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Backend
```

### 2. Backend Setup

```bash
cd TaskManagement.API

# Restore NuGet packages
dotnet restore

# Update database connection string in appsettings.json if needed
# Default: Server=(localdb)\\mssqllocaldb;Database=TaskManagementDb

# Apply database migrations
dotnet ef database update

# Run the API
dotnet run
```

The API will start at `https://localhost:7123` (or check console output for the actual port).

**Swagger UI** will be available at: `https://localhost:7123/swagger`

### 3. Frontend Setup

Open a new terminal:

```bash
cd Frontend/task-management-ui

# Install dependencies
npm install

# Start the development server
npm start
```

The React app will start at `http://localhost:3000`

## 🔌 API Endpoints

### Tasks Controller

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | Get all tasks |
| GET | `/api/tasks/{id}` | Get task by ID |
| GET | `/api/tasks/overdue` | Get overdue tasks |
| POST | `/api/tasks` | Create new task |
| PUT | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |

### Request/Response Examples

#### Create Task (POST)
```json
{
  "title": "Complete project documentation",
  "description": "Write comprehensive README and API documentation",
  "dueDate": "2024-12-31T23:59:59",
  "priority": 4,
  "fullName": "John Doe",
  "telephone": "+1-555-0123",
  "email": "john.doe@example.com"
}
```

#### Response (200 OK)
```json
{
  "id": 1,
  "title": "Complete project documentation",
  "description": "Write comprehensive README and API documentation",
  "dueDate": "2024-12-31T23:59:59Z",
  "priority": 4,
  "fullName": "John Doe",
  "telephone": "+1-555-0123",
  "email": "john.doe@example.com",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null,
  "isOverdue": false,
  "isCompleted": false
}
```

## 🎨 Frontend Features

### Components

1. **TaskList**: Displays all tasks in a responsive grid
   - Priority badges with color coding
   - Overdue indicators
   - Completion status
   - Edit and Delete actions

2. **TaskForm**: Create and edit tasks
   - Full field validation
   - Real-time error messages
   - Responsive layout
   - Date/time picker for due dates

3. **Redux Store**: Centralized state management
   - Async thunks for API calls
   - Loading states
   - Error handling
   - Optimistic updates

## ✅ Validation Rules

### Field Validations

| Field | Rules |
|-------|-------|
| Title | Required, Max 200 characters |
| Description | Required, Max 1000 characters |
| Due Date | Required, Must be a valid date |
| Priority | Required, Range 1-5 |
| Full Name | Required, Max 100 characters |
| Telephone | Required, Valid phone format, Max 20 characters |
| Email | Required, Valid email format, Max 100 characters |

## 🧪 Testing

### Backend Testing

```bash
cd TaskManagement.API
dotnet test
```

### Frontend Testing

```bash
cd Frontend/task-management-ui
npm test
```

Run with coverage:
```bash
npm test -- --coverage
```

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Ensure SQL Server LocalDB is installed
   - Check connection string in `appsettings.json`
   - Run `dotnet ef database update` again

2. **CORS Error in Frontend**
   - Verify backend API is running
   - Check CORS policy in `Program.cs`
   - Ensure frontend URL matches CORS configuration

3. **Port Already in Use**
   - Backend: Modify port in `Properties/launchSettings.json`
   - Frontend: Set `PORT=3001` in environment variable

4. **API URL Mismatch**
   - Update `API_BASE_URL` in `src/services/api.service.ts`
   - Match with backend's actual running port

## 📁 Project Structure

```
Backend/
├── TaskManagement.API/
│   ├── Controllers/
│   │   └── TasksController.cs
│   ├── Data/
│   │   └── TaskManagementDbContext.cs
│   ├── DTOs/
│   │   ├── CreateTaskDto.cs
│   │   ├── UpdateTaskDto.cs
│   │   └── TaskResponseDto.cs
│   ├── Models/
│   │   └── TaskItem.cs
│   ├── Migrations/
│   ├── Program.cs
│   └── appsettings.json

Frontend/
└── task-management-ui/
    ├── public/
    ├── src/
    │   ├── components/
    │   │   ├── TaskForm.tsx
    │   │   ├── TaskForm.css
    │   │   ├── TaskList.tsx
    │   │   └── TaskList.css
    │   ├── services/
    │   │   └── api.service.ts
    │   ├── store/
    │   │   ├── store.ts
    │   │   ├── taskSlice.ts
    │   │   └── hooks.ts
    │   ├── types/
    │   │   └── task.types.ts
    │   ├── App.tsx
    │   ├── App.css
    │   └── index.tsx
    └── package.json
```

## 🔐 Security Considerations

- All inputs are validated on both client and server
- SQL injection prevention through Entity Framework parameterization
- CORS configured for specific origins
- HTTPS enforced in production
- Input sanitization for XSS prevention

## 🎁 Bonus Feature: Windows Service + RabbitMQ

### Implementation Complete! ✅

The Windows Service monitors overdue tasks and sends reminders via RabbitMQ message queue.

#### Features:
- ✅ Background service running every 5 minutes
- ✅ Checks database for overdue tasks (`DueDate < Now` AND `IsCompleted = false`)
- ✅ Publishes reminders to RabbitMQ queue
- ✅ Subscribes to queue and logs each reminder
- ✅ Handles concurrent updates safely through message queuing
- ✅ Comprehensive logging to Windows Event Log

#### Quick Setup:

1. **Install RabbitMQ**:
   ```bash
   # Download and install from https://www.rabbitmq.com/download.html
   # Start RabbitMQ service
   rabbitmq-service.bat start
   ```

2. **Build and Install Service**:
   ```powershell
   # Run as Administrator
   cd TaskManagement.Service
   .\install-service.ps1
   ```

3. **View Logs**:
   - Open Event Viewer (`eventvwr.msc`)
   - Navigate to: Windows Logs → Application
   - Filter by Source: `TaskManagementService`

#### Log Output Example:
```
Information: Found 2 overdue tasks
Warning: TASK REMINDER: Hi John Doe, your Task is due - Task ID: 5, Title: 'Complete Project', Due Date: 12/30/2024
```

For detailed setup and configuration, see: [TaskManagement.Service/README.md](TaskManagement.Service/README.md)

## 📝 License

This project is created as a home assignment demonstration.

## 👥 Contributors

- Developer: [Your Name]
- Date: January 2024

## 📞 Support

For questions or issues, please refer to the project documentation or contact the development team.

---

## Quick Start Summary

```bash
# Backend
cd Backend/TaskManagement.API
dotnet restore
dotnet ef database update
dotnet run

# Frontend (new terminal)
cd Frontend/task-management-ui
npm install
npm start

# Access
Backend API: https://localhost:7123/swagger
Frontend: http://localhost:3000
```

**Note**: Make sure both backend and frontend are running simultaneously for the application to work properly.
