# Setup Checklist - Task Management System

Use this checklist to ensure everything is configured correctly.

## ✅ Prerequisites

### Required Software
- [ ] .NET 8 SDK installed
  - Verify: `dotnet --version` (should show 8.x.x)
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0

- [ ] Node.js installed (v18+)
  - Verify: `node --version` && `npm --version`
  - Download: https://nodejs.org/

- [ ] SQL Server LocalDB installed
  - Usually comes with Visual Studio
  - Verify: `sqllocaldb info`
  - Download: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

### Optional (For Bonus Feature)
- [ ] RabbitMQ installed
  - Download: https://www.rabbitmq.com/download.html
- [ ] Erlang installed (required by RabbitMQ)
  - Download: https://www.erlang.org/downloads

---

## 📦 Backend Setup

### 1. Restore Packages
```bash
cd Backend/TaskManagement.API
dotnet restore
```
- [ ] Packages restored successfully

### 2. Database Setup
```bash
dotnet ef database update
```
- [ ] Migration applied successfully
- [ ] Database "TaskManagementDb" created
- [ ] Tasks table exists with proper schema

**Verify in SSMS or VS SQL Object Explorer:**
- [ ] Table: Tasks
- [ ] Indexes: IX_Tasks_DueDate, IX_Tasks_Email, IX_Tasks_IsCompleted

### 3. Configuration Check
**File**: `appsettings.json`
- [ ] Connection string is correct
- [ ] Points to: `(localdb)\\mssqllocaldb`

### 4. Build & Run
```bash
dotnet build
dotnet run
```
- [ ] Build successful
- [ ] API starts without errors
- [ ] Swagger UI accessible at https://localhost:7123/swagger

**Test Swagger:**
- [ ] GET /api/tasks returns 200
- [ ] POST /api/tasks accepts new task
- [ ] Can see created task in database

---

## 🎨 Frontend Setup

### 1. Install Dependencies
```bash
cd Frontend/task-management-ui
npm install
```
- [ ] Dependencies installed (check for no errors)
- [ ] node_modules folder created

### 2. Configuration Check
**File**: `src/services/api.service.ts`
- [ ] API_BASE_URL matches backend URL
- [ ] Default: `https://localhost:7123/api`

### 3. Start Development Server
```bash
npm start
```
- [ ] React app starts without errors
- [ ] Opens browser to http://localhost:3000
- [ ] No console errors in browser

### 4. UI Tests
- [ ] "Create New Task" button visible
- [ ] Can click and see form
- [ ] Form has all fields (Title, Description, Due Date, Priority, Full Name, Phone, Email)
- [ ] Can submit form
- [ ] New task appears in list

---

## 🧪 Integration Testing

### Test 1: Create Task
- [ ] Fill all fields with valid data
- [ ] Click "Create Task"
- [ ] Task appears in the list
- [ ] No error messages

### Test 2: Edit Task
- [ ] Click "Edit" on a task
- [ ] Modify some fields
- [ ] Click "Update Task"
- [ ] Changes reflected in list

### Test 3: Delete Task
- [ ] Click "Delete" on a task
- [ ] Confirmation dialog appears
- [ ] Confirm deletion
- [ ] Task removed from list

### Test 4: Validation
- [ ] Try to submit empty form
- [ ] Error messages appear for required fields
- [ ] Enter invalid email
- [ ] Email validation error shows
- [ ] Enter invalid phone
- [ ] Phone validation error shows

### Test 5: Overdue Tasks
- [ ] Create task with past due date
- [ ] Task shows "Overdue" indicator
- [ ] Red border on task card

### Test 6: Complete Task
- [ ] Edit a task
- [ ] Check "Mark as completed"
- [ ] Save
- [ ] Task shows completed status
- [ ] Gray border on task card

---

## 🎁 Bonus: Windows Service Setup (Optional)

### 1. RabbitMQ Installation
```bash
rabbitmq-service.bat start
rabbitmq-plugins.bat enable rabbitmq_management
```
- [ ] RabbitMQ service running
- [ ] Management UI accessible: http://localhost:15672
- [ ] Can login with guest/guest

### 2. Service Build
```bash
cd TaskManagement.Service
dotnet restore
dotnet build
```
- [ ] Build successful
- [ ] No compilation errors

### 3. Service Installation
```powershell
# As Administrator
.\install-service.ps1
```
- [ ] Service installed successfully
- [ ] Service started without errors
- [ ] Visible in services.msc

### 4. Service Verification
```powershell
sc.exe query TaskManagementService
```
- [ ] State: RUNNING
- [ ] No errors in Event Viewer

### 5. Test Service
- [ ] Create task with past due date via API/UI
- [ ] Wait 5 minutes OR restart service
- [ ] Open Event Viewer (eventvwr.msc)
- [ ] Navigate to: Windows Logs → Application
- [ ] Filter by Source: TaskManagementService
- [ ] See log: "TASK REMINDER: Hi {Name}, your Task is due..."

**RabbitMQ Verification:**
- [ ] Open http://localhost:15672
- [ ] Navigate to Queues tab
- [ ] See "task-reminders" queue
- [ ] Messages published and consumed

---

## 🐛 Troubleshooting

### Backend Issues

**Database Connection Error:**
```bash
# Check LocalDB is running
sqllocaldb start mssqllocaldb

# Recreate database
dotnet ef database drop
dotnet ef database update
```

**Port Already in Use:**
- Edit `Properties/launchSettings.json`
- Change port number
- Update frontend API URL accordingly

### Frontend Issues

**npm install fails:**
```bash
npm cache clean --force
rm -rf node_modules package-lock.json
npm install
```

**CORS Error:**
- Verify backend is running
- Check CORS policy in `Program.cs`
- Ensure frontend URL matches CORS config

**Can't connect to API:**
- Check API URL in `api.service.ts`
- Verify backend port in URL
- Check browser network tab for actual error

### Service Issues

**Service Won't Start:**
- Check Event Viewer for specific error
- Verify RabbitMQ is running
- Check database connection string
- Ensure all dependencies are restored

**RabbitMQ Connection Failed:**
```bash
rabbitmq-service.bat restart
rabbitmqctl.bat status
```

---

## 📊 Final Verification

### Backend ✅
- [ ] API runs without errors
- [ ] Swagger UI works
- [ ] All CRUD operations functional
- [ ] Validation works
- [ ] Database updates correctly

### Frontend ✅
- [ ] UI loads correctly
- [ ] All features work
- [ ] Forms validate properly
- [ ] State management works
- [ ] No console errors

### Integration ✅
- [ ] Frontend communicates with backend
- [ ] Data persists in database
- [ ] Changes reflect immediately
- [ ] Error handling works

### Bonus ✅
- [ ] Windows Service running
- [ ] RabbitMQ connected
- [ ] Messages published/consumed
- [ ] Logs appear in Event Viewer

---

## 🎉 Success Criteria

Your setup is complete when:
1. ✅ Backend API runs and responds to requests
2. ✅ Frontend UI loads and can create/edit/delete tasks
3. ✅ Data persists in SQL Server database
4. ✅ Validation works on all fields
5. ✅ No errors in console or logs
6. ✅ (Bonus) Windows Service logs task reminders

---

## 📝 Notes

**Backend URL**: https://localhost:7123 (or check console for actual port)
**Frontend URL**: http://localhost:3000
**Swagger**: https://localhost:7123/swagger
**RabbitMQ UI**: http://localhost:15672

**Default Credentials:**
- RabbitMQ: guest / guest

---

## 🆘 Need Help?

1. **Check logs**: Console output, Event Viewer
2. **Verify connections**: Database, RabbitMQ
3. **Review documentation**: README.md, service README
4. **Check configuration**: appsettings.json, API URLs

---

**Status**: [ ] Complete [ ] In Progress [ ] Issues Found

**Notes**:
_______________________________________
_______________________________________
_______________________________________

**Completed By**: ________________ **Date**: __________
