using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyAndReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tasks",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskReminders_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskReminders_SentAt",
                table: "TaskReminders",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskReminders_TaskId",
                table: "TaskReminders",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskReminders_TaskId_SentAt",
                table: "TaskReminders",
                columns: new[] { "TaskId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskReminders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tasks");
        }
    }
}
