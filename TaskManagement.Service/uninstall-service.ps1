# Uninstall Task Management Service
# Run this script as Administrator

$serviceName = "TaskManagementService"

Write-Host "Uninstalling $serviceName..." -ForegroundColor Yellow

# Check if service exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    # Stop the service
    Write-Host "Stopping service..." -ForegroundColor Cyan
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # Delete the service
    Write-Host "Removing service..." -ForegroundColor Cyan
    sc.exe delete $serviceName
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service uninstalled successfully!" -ForegroundColor Green
    } else {
        Write-Host "Failed to uninstall service!" -ForegroundColor Red
    }
} else {
    Write-Host "Service not found!" -ForegroundColor Yellow
}
