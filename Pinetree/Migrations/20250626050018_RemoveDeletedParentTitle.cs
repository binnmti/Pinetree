using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeletedParentTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedParentTitle",
                table: "Pinecone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletedParentTitle",
                table: "Pinecone",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
