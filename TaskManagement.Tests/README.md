# TaskManagement.Tests

This project contains comprehensive unit and integration tests for the TaskManagement API.

## Test Structure

```
TaskManagement.Tests/
├── Controllers/
│   └── TasksControllerTests.cs          # Unit tests for TasksController
├── Integration/
│   └── TasksApiIntegrationTests.cs      # Integration tests for API endpoints
├── DTOs/
│   └── CreateTaskDtoValidationTests.cs  # Validation tests for DTOs
└── Models/
    └── TaskItemTests.cs                  # Unit tests for TaskItem model
```

## Testing Framework & Libraries

- **xUnit**: Testing framework
- **Moq**: Mocking library for unit tests
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing with WebApplicationFactory

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Tests with Detailed Output
```bash
dotnet test --verbosity detailed
```

### Run Tests and Generate Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~TasksControllerTests"
```

### Run Tests in Visual Studio
- Open Test Explorer (Test > Test Explorer)
- Click "Run All" to execute all tests
- Or right-click specific tests to run individually

## Test Coverage

### Unit Tests (TasksControllerTests.cs)
- ✅ GetTasks - Returns all tasks
- ✅ GetTasks - Returns empty list when no tasks
- ✅ GetTask - Returns specific task by ID
- ✅ GetTask - Returns 404 when task not found
- ✅ CreateTask - Creates new task with valid data
- ✅ UpdateTask - Updates existing task
- ✅ UpdateTask - Returns 404 for non-existent task
- ✅ DeleteTask - Deletes existing task
- ✅ DeleteTask - Returns 404 for non-existent task
- ✅ GetOverdueTasks - Returns only overdue incomplete tasks

### Integration Tests (TasksApiIntegrationTests.cs)
- ✅ GET /api/tasks - Returns success status
- ✅ POST /api/tasks - Creates task with valid data
- ✅ POST /api/tasks - Returns BadRequest with invalid data
- ✅ GET /api/tasks/{id} - Returns task when exists
- ✅ GET /api/tasks/{id} - Returns 404 when not found
- ✅ PUT /api/tasks/{id} - Updates task successfully
- ✅ DELETE /api/tasks/{id} - Deletes task successfully
- ✅ GET /api/tasks/overdue - Returns overdue tasks

### Model Tests (TaskItemTests.cs)
- ✅ IsOverdue property logic for future dates
- ✅ IsOverdue property logic for past dates
- ✅ IsOverdue property with completed tasks
- ✅ CreatedAt default value
- ✅ IsCompleted default value
- ✅ UpdatedAt default value

### Validation Tests (CreateTaskDtoValidationTests.cs)
- ✅ Valid DTO passes validation
- ✅ Empty title fails validation
- ✅ Invalid email fails validation
- ✅ Invalid priority range fails validation
- ✅ Title length exceeding maximum fails validation

## Best Practices

### Unit Tests
- Use in-memory database with unique names for each test
- Mock external dependencies (ILogger)
- Test one scenario per test method
- Use descriptive test names (Method_Scenario_ExpectedResult)

### Integration Tests
- Use WebApplicationFactory for real HTTP requests
- Each test gets a fresh database instance
- Test full request/response cycle
- Verify HTTP status codes and response content

## Adding New Tests

When adding new functionality:

1. **Add unit tests** in appropriate folder (Controllers, Models, DTOs)
2. **Add integration tests** for new API endpoints
3. **Follow AAA pattern**: Arrange, Act, Assert
4. **Use meaningful test names** that describe the scenario

### Example Test Template

```csharp
[Fact]
public async Task MethodName_WhenScenario_ExpectedResult()
{
    // Arrange
    // Set up test data and dependencies

    // Act
    // Execute the method being tested

    // Assert
    // Verify the expected outcome
}
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- No external dependencies required
- Uses in-memory database
- Fast execution time
- Isolated test runs

## Troubleshooting

### Tests Failing with Database Errors
- Ensure each test uses a unique database name: `Guid.NewGuid().ToString()`
- Check that in-memory database provider is properly configured

### Integration Tests Not Finding Program Class
- Ensure `public partial class Program { }` is added at the end of Program.cs
- Verify project reference is correctly set up

### Package Restore Issues
```bash
dotnet restore
dotnet clean
dotnet build
```
