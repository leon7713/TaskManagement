using System.ComponentModel.DataAnnotations;
using TaskManagement.API.DTOs;
using Xunit;

namespace TaskManagement.Tests.DTOs
{
    public class CreateTaskDtoValidationTests
    {
        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void CreateTaskDto_WithValidData_PassesValidation()
        {
            // Arrange
            var dto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CreateTaskDto_WithEmptyTitle_FailsValidation()
        {
            // Arrange
            var dto = new CreateTaskDto
            {
                Title = "",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Title"));
        }

        [Fact]
        public void CreateTaskDto_WithInvalidEmail_FailsValidation()
        {
            // Arrange
            var dto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "invalid-email"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Email"));
        }

        [Fact]
        public void CreateTaskDto_WithInvalidPriority_FailsValidation()
        {
            // Arrange
            var dto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 10,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Priority"));
        }

        [Fact]
        public void CreateTaskDto_WithTooLongTitle_FailsValidation()
        {
            // Arrange
            var dto = new CreateTaskDto
            {
                Title = new string('A', 201),
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "1234567890",
                Email = "john@example.com"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Title"));
        }
    }
}
