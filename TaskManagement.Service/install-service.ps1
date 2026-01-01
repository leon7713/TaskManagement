# Install Task Management Service
# Run this script as Administrator

$serviceName = "TaskManagementService"
$displayName = "Task Management Reminder Service"
$description = "Monitors overdue tasks and sends reminders via RabbitMQ"
$binaryPath = "$PSScriptRoot\publish\TaskManagement.Service.exe"

Write-Host "Installing $displayName..." -ForegroundColor Green

# Check if service already exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
}

# Build and publish the service
Write-Host "Building service..." -ForegroundColor Cyan
dotnet publish -c Release -o .\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Install the service
Write-Host "Installing service..." -ForegroundColor Cyan
sc.exe create $serviceName binPath= $binaryPath start= auto DisplayName= $displayName

if ($LASTEXITCODE -eq 0) {
    sc.exe description $serviceName $description
    Write-Host "Service installed successfully!" -ForegroundColor Green
    
    # Start the service
    $startService = Read-Host "Do you want to start the service now? (Y/N)"
    if ($startService -eq 'Y' -or $startService -eq 'y') {
        Write-Host "Starting service..." -ForegroundColor Cyan
        sc.exe start $serviceName
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Service started successfully!" -ForegroundColor Green
        } else {
            Write-Host "Failed to start service. Check Event Viewer for details." -ForegroundColor Red
        }
    }
} else {
    Write-Host "Failed to install service!" -ForegroundColor Red
    exit 1
}

Write-Host "`nService Management Commands:" -ForegroundColor Yellow
Write-Host "  Start:   sc.exe start $serviceName" -ForegroundColor Gray
Write-Host "  Stop:    sc.exe stop $serviceName" -ForegroundColor Gray
Write-Host "  Status:  sc.exe query $serviceName" -ForegroundColor Gray
Write-Host "  Delete:  sc.exe delete $serviceName" -ForegroundColor Gray
