using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditCategoryAndPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditCategory",
                table: "AuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "AuditLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditCategory",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "AuditLogs");
        }
    }
}
