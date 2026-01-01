# Quick Start Guide - Task Management System

## 🚀 Get Up and Running in 5 Minutes!

### Step 1: Backend API (2 minutes)

```bash
# Navigate to API project
cd Backend/TaskManagement.API

# Restore packages
dotnet restore

# Create database
dotnet ef database update

# Run the API
dotnet run
```

✅ **Backend running at**: https://localhost:7123/swagger

---

### Step 2: Frontend React App (2 minutes)

**Open a NEW terminal window:**

```bash
# Navigate to frontend
cd Frontend/task-management-ui

# Install dependencies (first time only)
npm install

# Start the app
npm start
```

✅ **Frontend running at**: http://localhost:3000

---

### Step 3: Test the Application (1 minute)

1. **Open browser**: http://localhost:3000
2. **Click "Create New Task"**
3. **Fill in the form**:
   - Title: My First Task
   - Description: Testing the app
   - Due Date: (pick a future date)
   - Priority: 3
   - Full Name: John Doe
   - Telephone: +1-555-0123
   - Email: john@example.com
4. **Click "Create Task"**
5. **See your task appear in the list!**

---

## ✅ That's It! You're Done!

### What You Can Do Now:
- ✅ Create tasks
- ✅ Edit tasks
- ✅ Delete tasks
- ✅ View all tasks
- ✅ See overdue indicators
- ✅ Mark tasks complete

---

## 🎁 Bonus: Windows Service (Optional)

### Prerequisites:
1. **Install RabbitMQ**:
   - Download: https://www.rabbitmq.com/download.html
   - Install with default settings
   - Start service: `rabbitmq-service.bat start`

2. **Install the Service**:
   ```powershell
   # Run PowerShell as Administrator
   cd TaskManagement.Service
   .\install-service.ps1
   ```

3. **Test It**:
   - Create a task with a past due date
   - Wait 5 minutes (or restart service)
   - Check Event Viewer for reminder logs

---

## 🔧 Troubleshooting

### Backend won't start?
- Make sure SQL Server LocalDB is installed
- Check port 7123 is available

### Frontend won't start?
- Make sure Node.js is installed
- Try: `npm cache clean --force` then `npm install`

### Can't create tasks?
- Verify backend is running
- Check browser console for errors
- Update API URL in `src/services/api.service.ts` if backend port changed

---

## 📚 Need More Info?

- **Full Documentation**: See [README.md](README.md)
- **API Details**: Visit https://localhost:7123/swagger
- **Service Guide**: See [TaskManagement.Service/README.md](TaskManagement.Service/README.md)
- **Project Summary**: See [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)

---

## 🎯 Important API URL Configuration

If your backend runs on a different port, update this file:

**File**: `Frontend/task-management-ui/src/services/api.service.ts`

```typescript
const API_BASE_URL = 'https://localhost:YOUR_PORT/api';
```

Check your backend console output for the actual port number!

---

## 💡 Pro Tips

1. **Keep both terminals open** - You need backend AND frontend running
2. **Use Swagger** - Great for testing API directly: https://localhost:7123/swagger
3. **Check the console** - Both terminals show useful debug info
4. **Overdue tasks** - Create task with past date to see overdue indicator
5. **RabbitMQ UI** - Access at http://localhost:15672 (guest/guest)

---

**Happy Task Managing! 🎉**
