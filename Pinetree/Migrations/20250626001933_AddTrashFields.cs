using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class AddTrashFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeleteType",
                table: "Pinecone",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Pinecone",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedParentTitle",
                table: "Pinecone",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pinecone",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteType",
                table: "Pinecone");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Pinecone");

            migrationBuilder.DropColumn(
                name: "DeletedParentTitle",
                table: "Pinecone");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pinecone");
        }
    }
}
